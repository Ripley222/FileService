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

public sealed class RemoveFileRequestValidator : AbstractValidator<RemoveFileRequest>
{
    public RemoveFileRequestValidator()
    {
        RuleFor(x => x.MediaAssetId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("MediaAssetId"));
    }
}

public sealed class RemoveEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("files/{mediaAssetId:guid}", async (
                Guid mediaAssetId,
                [FromServices] DeleteFileHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new RemoveFileRequest(mediaAssetId), cancellationToken);

                return Results.Ok(result.Value);
            })
            .DisableAntiforgery();
    }
}

public sealed class DeleteFileHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly IValidator<RemoveFileRequest> _validator;
    private readonly ILogger<DeleteFileHandler> _logger;

    public DeleteFileHandler(
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        IValidator<RemoveFileRequest> validator,
        ILogger<DeleteFileHandler> logger)
    {
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, ErrorList>> Handle(
        RemoveFileRequest request, CancellationToken cancellationToken)
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

        var removeFileInS3Result = await _s3Provider.RemoveFileAsync(key!, cancellationToken);
        if (removeFileInS3Result.IsFailure)
            return removeFileInS3Result.Error.ToErrors();
        
        var markDeletedResult = mediaAssetResult.Value.MarkDeleted(DateTime.UtcNow);
        if (markDeletedResult.IsFailure)
            return markDeletedResult.Error.ToErrors();

        var saveChangesResult = await _mediaRepository.SaveChangesAsync(cancellationToken);
        if (saveChangesResult.IsFailure)
            return saveChangesResult.Error.ToErrors();

        _logger.LogInformation("File with id {fileId} deleted.", request.MediaAssetId);

        return removeFileInS3Result.Value;
    }
}