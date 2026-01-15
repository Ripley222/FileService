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

public sealed class GenerateUploadUrlRequestValidator : AbstractValidator<GenerateUploadUrlRequest>
{
    public GenerateUploadUrlRequestValidator()
    {
        RuleFor(g => g.MediaAssetId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("MediaAssetId"));
    }
}

public sealed class GenerateUploadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{fileId:guid}/upload-url", async (
            Guid mediaAssetId,
            [FromServices] GeneratePresignedUrlHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DownloadPresignedUrlRequest(mediaAssetId), cancellationToken);

            return Results.Ok(result.Value);
        });
    }
}

public sealed class GenerateUploadUrlHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly IValidator<GenerateUploadUrlRequest> _validator;
    private readonly ILogger<GenerateUploadUrlHandler> _logger;
    
    public GenerateUploadUrlHandler(
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        IValidator<GenerateUploadUrlRequest> validator,
        ILogger<GenerateUploadUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, ErrorList>> Handle(
        GenerateUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();
        
        var mediaAssetResult = await _mediaRepository.GetByIdAsync(request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();
        
        var key = mediaAssetResult.Value.RawKey!.IsEmpty() 
            ? mediaAssetResult.Value.FinalKey
            : mediaAssetResult.Value.RawKey;
        
        var uploadUrlResult = await _s3Provider
            .GenerateUploadUrlAsync(key!, mediaAssetResult.Value.MediaData, cancellationToken);
        
        if (uploadUrlResult.IsFailure)
            return uploadUrlResult.Error.ToErrors();
        
        _logger.LogInformation("Generated upload URL for file with id {fileId}.", request.MediaAssetId);

        return uploadUrlResult.Value;
    }
}