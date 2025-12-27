using CSharpFunctionalExtensions;
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

public sealed record DownloadFileRequest(Guid FileId, string Path);

public sealed class DownloadFileRequestValidator : AbstractValidator<DownloadFileRequest>
{
    public DownloadFileRequestValidator()
    {
        RuleFor(d => d.FileId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("FileId"));

        RuleFor(d => d.Path)
            .NotEmpty()
            .WithError(Errors.General.ValueIsInvalid("Path"));
    }
}

public sealed class DownloadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{fileId:guid}", async (
            Guid fileId,
            [FromQuery] string path,
            [FromServices] DownloadFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new DownloadFileRequest(fileId, path), cancellationToken);

            return Results.Ok(result.Value);
        });
    }
}

public sealed class DownloadFileHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly IValidator<DownloadFileRequest> _validator;
    private readonly ILogger<DownloadFileHandler> _logger;

    public DownloadFileHandler(
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        IValidator<DownloadFileRequest> validator,
        ILogger<DownloadFileHandler> logger)
    {
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, ErrorList>> Handle(
        DownloadFileRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAssetResult = await _mediaRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        var key = mediaAssetResult.Value.RawKey!.IsEmpty()
            ? mediaAssetResult.Value.FinalKey
            : mediaAssetResult.Value.RawKey;

        var downloadResult = await _s3Provider.DownloadFileAsync(key!, request.Path, cancellationToken);
        if (downloadResult.IsFailure)
            return downloadResult.Error.ToErrors();

        _logger.LogInformation("File with id {fileId} downloaded.", request.FileId);

        return downloadResult.Value;
    }
}