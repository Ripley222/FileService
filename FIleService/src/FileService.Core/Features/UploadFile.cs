using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Core.FileProviders;
using FileService.Core.Repositories;
using FileService.Domain.Entities;
using FileService.Domain.Entities.Enums;
using FileService.Domain.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Core.Validation;
using Shared.Framework.Endpoints;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Features;

public sealed class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(u => u.FileName)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("FileName"));

        RuleFor(u => u.ContentType)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("ContentType"));

        RuleFor(u => u.Size)
            .Must(s => s > 0)
            .WithError(Errors.General.ValueIsInvalid("Size"));
    }
}

public sealed class UploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files", async (
            IFormFile file,
            [FromServices] UploadFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var fileName = file.FileName;
            var contentType = file.ContentType;
            await using var stream = file.OpenReadStream();

            var request = new UploadFileRequest(
                fileName,
                stream,
                contentType,
                file.Length);

            UnitResult<ErrorList> result = await handler.Handle(request, cancellationToken);
            
            return new EndpointResult<bool>(result);
        }).DisableAntiforgery();
    }
}

public sealed class UploadFileHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IS3Options _s3Options;
    private readonly IMediaRepository _mediaRepository;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly IValidator<UploadFileRequest> _validator;
    private readonly ILogger<UploadFileHandler> _logger;

    public UploadFileHandler(
        IS3Provider s3Provider,
        IS3Options s3Options,
        IMediaRepository mediaRepository,
        IChunkSizeCalculator chunkSizeCalculator,
        IValidator<UploadFileRequest> validator,
        ILogger<UploadFileHandler> logger)
    {
        _s3Provider = s3Provider;
        _s3Options = s3Options;
        _mediaRepository = mediaRepository;
        _chunkSizeCalculator = chunkSizeCalculator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UnitResult<ErrorList>> Handle(
        UploadFileRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var fileNameResult = FileName.Create(request.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error.ToErrors();

        var contentTypeResult = ContentType.Create(request.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error.ToErrors();

        var calculate = _chunkSizeCalculator.ChunksCalculator(
            request.Size,
            _s3Options.RecommendedChunksSizeBytes,
            _s3Options.MaxChunks);

        var mediaDataResult = MediaData.Create(
            fileNameResult.Value, contentTypeResult.Value, request.Size, calculate.Value.Item2);

        if (mediaDataResult.IsFailure)
            return mediaDataResult.Error.ToErrors();

        var mediaAssetResult = MediaAsset.CreateForUpload(mediaDataResult.Value, request.ContentType.ToAssetType());
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        var saveResult = await _mediaRepository.AddAsync(mediaAssetResult.Value, cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error.ToErrors();

        var storageKey = mediaAssetResult.Value.RawKey ?? mediaAssetResult.Value.FinalKey;

        var uploadResult = await _s3Provider.UploadFileAsync(
            storageKey!,
            request.Stream,
            mediaDataResult.Value,
            cancellationToken);

        if (uploadResult.IsFailure)
            return uploadResult.Error.ToErrors();

        _logger.LogInformation("Uploaded new file with id {fileId}", mediaAssetResult.Value.Id);

        return UnitResult.Success<ErrorList>();
    }
}