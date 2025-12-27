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

public sealed record DownloadPresignedUrlsRequest(IEnumerable<Guid> FileIds);

public sealed class DownloadPresignedUrlsRequestValidator : AbstractValidator<DownloadPresignedUrlsRequest>
{
    public DownloadPresignedUrlsRequestValidator()
    {
        RuleForEach(d => d.FileIds)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("FileId"));
    }
}

public sealed class GenerateDownloadUrlsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/download-urls", async (
            Guid[] fileIds,
            [FromServices] GeneratePresignedUrlsHandler presignedUrlsHandler,
            CancellationToken cancellationToken) =>
        {
            var result = await presignedUrlsHandler
                .Handle(new DownloadPresignedUrlsRequest(fileIds), cancellationToken);

            return Results.Ok(result.Value);
        }).DisableAntiforgery();
    }
}

public sealed class GeneratePresignedUrlsHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly IValidator<DownloadPresignedUrlsRequest> _validator;
    private readonly ILogger<GeneratePresignedUrlHandler> _logger;

    public GeneratePresignedUrlsHandler(
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        IValidator<DownloadPresignedUrlsRequest> validator,
        ILogger<GeneratePresignedUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<string>, ErrorList>> Handle(
        DownloadPresignedUrlsRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAssetsResult = await _mediaRepository.GetByIdsAsync(request.FileIds, cancellationToken);
        if (mediaAssetsResult.IsFailure)
            return mediaAssetsResult.Error.ToErrors();

        var keys = mediaAssetsResult.Value.Select(m => m.RawKey!.IsEmpty() 
            ? m.FinalKey
            : m.RawKey).ToList();

        var presignedUrlResult = await _s3Provider.GenerateDownloadUrlsAsync(keys!);
        if (presignedUrlResult.IsFailure)
            return presignedUrlResult.Error.ToErrors();

        _logger.LogInformation("Generated download URLs for many files.");

        return Result.Success<IReadOnlyList<string>, ErrorList>(presignedUrlResult.Value);
    }
}