using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Core.FileProviders;
using FileService.Core.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Core.Validation;
using Shared.Framework.Endpoints;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Features;

public sealed record AbortMultipartUploadResponse(bool Success);

public sealed class AbortMultipartUploadRequestValidator : AbstractValidator<AbortMultipartUploadRequest>
{
    public AbortMultipartUploadRequestValidator()
    {
        RuleFor(a => a.MediaAssetId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("MediaAssetId"));

        RuleFor(a => a.UploadId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("UploadId"));
    }
}

public sealed class AbortMultipartUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files/multipart/abort", async (
                [FromBody] AbortMultipartUploadRequest request,
                [FromServices] AbortMultipartUploadHandler handler,
                CancellationToken cancellationToken) =>
            {
                Result<AbortMultipartUploadResponse, ErrorList> result = await handler.Handle(request, cancellationToken);

                return new EndpointResult<AbortMultipartUploadResponse>(result);
            })
            .DisableAntiforgery();
    }
}

public sealed class AbortMultipartUploadHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<AbortMultipartUploadRequest> _validator;
    private readonly ILogger<AbortMultipartUploadHandler> _logger;

    public AbortMultipartUploadHandler(
        IMediaRepository mediaRepository,
        IS3Provider s3Provider,
        IValidator<AbortMultipartUploadRequest> validator,
        ILogger<AbortMultipartUploadHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AbortMultipartUploadResponse, ErrorList>> Handle(
        AbortMultipartUploadRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAssetResult = await _mediaRepository.GetByIdAsync(request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        var storageKey = mediaAssetResult.Value.RawKey!.IsEmpty()
            ? mediaAssetResult.Value.FinalKey
            : mediaAssetResult.Value.RawKey;

        var abortMultipartUploadResult = await _s3Provider.AbortMultipartUploadAsync(
            storageKey!,
            request.UploadId,
            cancellationToken);

        if (abortMultipartUploadResult.IsFailure)
            return abortMultipartUploadResult.Error.ToErrors();

        var markFailedResult = mediaAssetResult.Value.MarkFailed(DateTime.UtcNow);
        if (markFailedResult.IsFailure)
            return markFailedResult.Error.ToErrors();

        var saveChangesResult = await _mediaRepository.SaveChangesAsync(cancellationToken);
        if (saveChangesResult.IsFailure)
            return saveChangesResult.Error.ToErrors();
        
        _logger.LogInformation("Abort multipart uploading file");

        return new AbortMultipartUploadResponse(true);
    }
}