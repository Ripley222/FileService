using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
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

public sealed class CompleteMultipartUploadRequestValidator : AbstractValidator<CompleteMultipartUploadRequest>
{
    public CompleteMultipartUploadRequestValidator()
    {
        RuleFor(c => c.MediaAssetId)
            .Must(m => m != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("MediaAssetId"));

        RuleFor(c => c.UploadId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("UploadId"));

        RuleForEach(c => c.PartETags)
            .Must(p => p.PartNumber > 0)
            .WithError(Errors.General.ValueIsInvalid("PartNumber"))
            .Must(p => !string.IsNullOrWhiteSpace(p.ETag))
            .WithError(Errors.General.ValueIsInvalid("ETag"));
    }
}

public sealed class CompleteMultipartUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files/multipart/complete", async (
                [FromBody] CompleteMultipartUploadRequest request,
                [FromServices] CompleteMultipartUploadHandler handler,
                CancellationToken cancellationToken) =>
            {
                Result<CompleteMultipartUploadResponse, ErrorList> result = await handler.Handler(request, cancellationToken);

                return new EndpointResult<CompleteMultipartUploadResponse>(result);
            })
            .DisableAntiforgery();
    }
}

public sealed class CompleteMultipartUploadHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<CompleteMultipartUploadRequest> _validator;
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;

    public CompleteMultipartUploadHandler(
        IMediaRepository mediaRepository,
        IS3Provider s3Provider,
        IValidator<CompleteMultipartUploadRequest> validator,
        ILogger<CompleteMultipartUploadHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CompleteMultipartUploadResponse, ErrorList>> Handler(
        CompleteMultipartUploadRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAssetResult = await _mediaRepository.GetByIdAsync(request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        var storageKey = mediaAssetResult.Value.RawKey ?? mediaAssetResult.Value.FinalKey;

        var completeMultipartUploadResult = await _s3Provider.CompleteMultipartUploadAsync(
            storageKey!,
            request.UploadId,
            request.PartETags,
            cancellationToken);

        if (completeMultipartUploadResult.IsFailure)
            return completeMultipartUploadResult.Error.ToErrors();

        var markUploadedResult = mediaAssetResult.Value.MarkUploaded(DateTime.UtcNow);
        if (markUploadedResult.IsFailure)
            return markUploadedResult.Error.ToErrors();

        var saveChangesResult = await _mediaRepository.SaveChangesAsync(cancellationToken);
        if (saveChangesResult.IsFailure)
            return saveChangesResult.Error.ToErrors();

        _logger.LogInformation("Success complete multipart upload file");

        return new CompleteMultipartUploadResponse(mediaAssetResult.Value.Id);
    }
}