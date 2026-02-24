using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
using FileService.Core.FileProviders;
using FileService.Core.Repositories;
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

public sealed class ChunkUploadUrlRequestValidator : AbstractValidator<ChunkUploadUrlRequest>
{
    public ChunkUploadUrlRequestValidator()
    {
        RuleFor(c => c.MediaAssetId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("MediaAssetId"));

        RuleFor(c => c.UploadId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("UploadId"));

        RuleFor(c => c.PartNumber)
            .Must(p => p > 0)
            .WithError(Errors.General.ValueIsInvalid("PartNumber"));
    }
}

public sealed class ChunkUploadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files/multipart/url", async (
                [FromBody] ChunkUploadUrlRequest request,
                [FromServices] GenerateChunkUploadUrlHandler handler,
                CancellationToken cancellationToken) =>
            {
                Result<ChunkUploadUrlResponse, ErrorList> result = await handler.Handle(request, cancellationToken);

                return new EndpointResult<ChunkUploadUrlResponse>(result);
            })
            .DisableAntiforgery();
    }
}

public sealed class GenerateChunkUploadUrlHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<ChunkUploadUrlRequest> _validator;
    private readonly ILogger<GenerateChunkUploadUrlHandler> _logger;

    public GenerateChunkUploadUrlHandler(
        IMediaRepository mediaRepository,
        IS3Provider s3Provider,
        IValidator<ChunkUploadUrlRequest> validator,
        ILogger<GenerateChunkUploadUrlHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ChunkUploadUrlResponse, ErrorList>> Handle(
        ChunkUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAssetResult = await _mediaRepository.GetByIdAsync(request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        var storageKey = mediaAssetResult.Value.RawKey ?? mediaAssetResult.Value.FinalKey;

        var chunkUploadUrlResult = await _s3Provider.CreateChunkUploadUrlAsync(
            storageKey!,
            request.UploadId,
            request.PartNumber,
            cancellationToken);

        if (chunkUploadUrlResult.IsFailure)
            return chunkUploadUrlResult.Error.ToErrors();

        _logger.LogInformation("Success generate upload URl for chunk file");

        return new ChunkUploadUrlResponse(
            chunkUploadUrlResult.Value,
            request.PartNumber);
    }
}