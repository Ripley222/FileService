using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Core.Endpoints;
using FileService.Core.FileProviders;
using FileService.Core.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Features;

public sealed class DownloadPresignedUrlRequestValidator : AbstractValidator<DownloadPresignedUrlRequest>
{
    public DownloadPresignedUrlRequestValidator()
    {
        RuleFor(d => d.MediaAssetId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsRequired("MediaAssetId"));
    }
}

public sealed class GenerateDownloadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{fileId:guid}/download-url", async (
            Guid mediaAssetId,
            [FromServices] GeneratePresignedUrlHandler presignedUrlHandler,
            CancellationToken cancellationToken) =>
        {
            var result = await presignedUrlHandler.Handle(new DownloadPresignedUrlRequest(mediaAssetId), cancellationToken);

            return Results.Ok(result.Value);
        }).DisableAntiforgery();
    }
}

public sealed class GeneratePresignedUrlHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly IValidator<DownloadPresignedUrlRequest> _validator;
    private readonly ILogger<GeneratePresignedUrlHandler> _logger;

    public GeneratePresignedUrlHandler(
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        IValidator<DownloadPresignedUrlRequest> validator,
        ILogger<GeneratePresignedUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, ErrorList>> Handle(
        DownloadPresignedUrlRequest request, CancellationToken cancellationToken)
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

        var presignedUrlResult = await _s3Provider.GenerateDownloadUrlAsync(storageKey!);
        if (presignedUrlResult.IsFailure)
            return presignedUrlResult.Error.ToErrors();

        _logger.LogInformation("Generated download URL for file with id {fileId}.", request.MediaAssetId);

        return presignedUrlResult.Value;
    }
}