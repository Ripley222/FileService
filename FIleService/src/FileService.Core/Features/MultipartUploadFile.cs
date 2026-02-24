using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
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

public sealed class MultipartUploadRequestValidator : AbstractValidator<MultipartUploadRequest>
{
    public MultipartUploadRequestValidator()
    {
        RuleFor(m => m.FileName)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("FileName"));

        RuleFor(m => m.ContentType)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("ContentType"));

        RuleFor(m => m.Size)
            .Must(s => s > 0)
            .WithError(Errors.General.ValueIsInvalid("Size"));

        RuleFor(m => m.Context)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("Context"));

        RuleFor(m => m.ContextId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("ContextId"));
    }
}

public sealed class MultipartUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files/multipart/start", async (
                IFormFile file,
                [FromQuery] Guid contextId,
                [FromQuery] string context,
                [FromServices] MultipartUploadHandler handler,
                CancellationToken cancellationToken) =>
            {
                var request = new MultipartUploadRequest(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    context,
                    contextId);

                Result<MultipartUploadResponse, ErrorList> result = await handler.Handle(request, cancellationToken);

                return new EndpointResult<MultipartUploadResponse>(result);
            })
            .DisableAntiforgery();
    }
}

public sealed class MultipartUploadHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IS3Provider _s3Provider;
    private readonly IS3Options _s3Options;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly ILogger<MultipartUploadHandler> _logger;

    public MultipartUploadHandler(
        IMediaRepository mediaRepository,
        IS3Provider s3Provider,
        IS3Options options,
        IChunkSizeCalculator chunkSizeCalculator,
        ILogger<MultipartUploadHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _s3Provider = s3Provider;
        _s3Options = options;
        _chunkSizeCalculator = chunkSizeCalculator;
        _logger = logger;
    }

    public async Task<Result<MultipartUploadResponse, ErrorList>> Handle(
        MultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        var fileNameResult = FileName.Create(request.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error.ToErrors();

        var contentTypeResult = ContentType.Create(request.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error.ToErrors();

        var chunksCalculateResult = _chunkSizeCalculator.ChunksCalculator(
            request.Size,
            _s3Options.RecommendedChunksSizeBytes,
            _s3Options.MaxChunks);

        if (chunksCalculateResult.IsFailure)
            return chunksCalculateResult.Error.ToErrors();

        var mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            request.Size,
            chunksCalculateResult.Value.TotalChunks);

        if (mediaDataResult.IsFailure)
            return mediaDataResult.Error.ToErrors();

        var mediaAssetResult = MediaAsset.CreateForUpload(mediaDataResult.Value, request.ContentType.ToAssetType());
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        var storageKey = mediaAssetResult.Value.RawKey ?? mediaAssetResult.Value.FinalKey;

        var multipartUploadResult = await _s3Provider.StartMultipartUploadAsync(
            storageKey!,
            mediaDataResult.Value,
            cancellationToken);

        if (multipartUploadResult.IsFailure)
            return multipartUploadResult.Error.ToErrors();

        var saveInDatabaseResult = await _mediaRepository.AddAsync(mediaAssetResult.Value, cancellationToken);
        if (saveInDatabaseResult.IsFailure)
            return saveInDatabaseResult.Error.ToErrors();

        var presignedUrlsResult = await _s3Provider.GenerateAllChunkUploadUrlsAsync(
            storageKey!,
            multipartUploadResult.Value,
            chunksCalculateResult.Value.TotalChunks,
            cancellationToken);

        if (presignedUrlsResult.IsFailure)
            return presignedUrlsResult.Error.ToErrors();

        _logger.LogInformation("Started multipart uploading file");

        return new MultipartUploadResponse(
            mediaAssetResult.Value.Id,
            multipartUploadResult.Value,
            presignedUrlsResult.Value,
            chunksCalculateResult.Value.ChunkSize);
    }
}