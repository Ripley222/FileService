using CSharpFunctionalExtensions;
using FileService.Contracts.DTOs;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
using FileService.Core.Database;
using FileService.Core.FileProviders;
using FileService.Domain.Entities.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Core.Validation;
using Shared.Framework.Endpoints;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Features;

public sealed class GetMediaAssetsInfoRequestValidator : AbstractValidator<GetMediaAssetsInfoRequest>
{
    public GetMediaAssetsInfoRequestValidator()
    {
        RuleForEach(g => g.MediaAssetIds)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsInvalid("MediaAssetId"));
    }
}

public sealed class GetMediaAssetsInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files/batch", async (
            [FromBody] GetMediaAssetsInfoRequest request,
            [FromServices] GetMediaAssetsInfoHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<GetMediaAssetsInfoResponse, ErrorList> result = await handler.Handle(request, cancellationToken);
            
            return new EndpointResult<GetMediaAssetsInfoResponse>(result);
        })
        .DisableAntiforgery();
    }
}

public sealed class GetMediaAssetsInfoHandler
{
    private readonly IReadDbContext _readDbContext;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetMediaAssetsInfoRequest> _validator;
    private readonly ILogger<GetMediaAssetsInfoHandler> _logger;

    public GetMediaAssetsInfoHandler(
        IReadDbContext readDbContext,
        IS3Provider s3Provider,
        IValidator<GetMediaAssetsInfoRequest> validator,
        ILogger<GetMediaAssetsInfoHandler> logger)
    {
        _readDbContext = readDbContext;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<GetMediaAssetsInfoResponse, ErrorList>> Handle(
        GetMediaAssetsInfoRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAssetsList = await _readDbContext.MediaAssetsQuery
            .Where(m => request.MediaAssetIds.Contains(m.Id) && m.Status != Status.Deleted)
            .ToListAsync(cancellationToken);

        var storageKeyList = mediaAssetsList.Select(m =>
            m.RawKey!.IsEmpty() ? m.FinalKey : m.RawKey).ToList();

        var downloadUrlsResult = await _s3Provider.GenerateDownloadUrlsAsync(storageKeyList!);
        if (downloadUrlsResult.IsFailure)
            return downloadUrlsResult.Error.ToErrors();

        var mediaAssetDtos =
            from mediaAsset in mediaAssetsList
            join url in downloadUrlsResult.Value on mediaAsset.RawKey.IsEmpty()
                ? mediaAsset.FinalKey
                : mediaAsset.RawKey! equals url.StorageKey
            select new MediaAssetDto(
                mediaAsset.Id,
                mediaAsset.Status.ToString(),
                mediaAsset.AssetType.ToString(),
                mediaAsset.CreatedAt,
                mediaAsset.UpdatedAt,
                new FileInfoDto(
                    string.Concat(
                        mediaAsset.MediaData.FileName.Name,
                        mediaAsset.MediaData.FileName.Extension),
                    mediaAsset.MediaData.ContentType.Value,
                    mediaAsset.MediaData.Size),
                url.DownloadUrl);

        _logger.LogInformation("Get media assets info");

        return new GetMediaAssetsInfoResponse(mediaAssetDtos.ToList());
    }
}