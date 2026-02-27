using CSharpFunctionalExtensions;
using FileService.Contracts.DTOs;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
using FileService.Core.Database;
using FileService.Core.FileProviders;
using FileService.Domain.Entities.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Core.Validation;
using Shared.Framework.Endpoints;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Features;

public sealed class GetMediaAssetInfoRequestValidator : AbstractValidator<GetMediaAssetInfoRequest>
{
    public GetMediaAssetInfoRequestValidator()
    {
        RuleFor(g => g.MediaAssetId)
            .Must(id => id != Guid.Empty)
            .WithError(Errors.General.ValueIsRequired("MediaAssetId"));
    }
}

public sealed class GetMediaAssetInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{mediaAssetId:guid}", async (
            Guid mediaAssetId,
            [FromServices] GetMediaAssetInfoHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<GetMediaAssetInfoResponse, ErrorList> result = await handler
                .Handle(new GetMediaAssetInfoRequest(mediaAssetId), cancellationToken);

            return new EndpointResult<GetMediaAssetInfoResponse>(result);
        }).DisableAntiforgery();
    }
}

public sealed class GetMediaAssetInfoHandler
{
    private readonly IReadDbContext _readDbContext;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetMediaAssetInfoRequest> _validator;
    private readonly ILogger<GeneratePresignedUrlHandler> _logger;

    public GetMediaAssetInfoHandler(
        IReadDbContext readDbContext,
        IS3Provider s3Provider,
        IValidator<GetMediaAssetInfoRequest> validator,
        ILogger<GeneratePresignedUrlHandler> logger)
    {
        _readDbContext = readDbContext;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<GetMediaAssetInfoResponse, ErrorList>> Handle(
        GetMediaAssetInfoRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.GetErrors();

        var mediaAsset = await _readDbContext.MediaAssetsQuery
            .Where(m => m.Status != Status.Deleted)
            .FirstOrDefaultAsync(m => m.Id == request.MediaAssetId, cancellationToken);

        if (mediaAsset is null)
            return Errors.General.NotFound(request.MediaAssetId).ToErrors();

        var storageKey = mediaAsset.RawKey!.IsEmpty()
            ? mediaAsset.FinalKey
            : mediaAsset.RawKey;

        string presignedUrl = string.Empty;
        if (mediaAsset.Status == Status.Ready)
        {
            var presignedUrlResult = await _s3Provider.GenerateDownloadUrlAsync(storageKey!);
            if (presignedUrlResult.IsFailure)
                return presignedUrlResult.Error.ToErrors();

            presignedUrl = presignedUrlResult.Value;
        }

        var response = new GetMediaAssetInfoResponse(
            new MediaAssetDto(
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
                presignedUrl));

        _logger.LogInformation("Getting info for media asset with id: {mediaAssetId}.", mediaAsset.Id);

        return response;
    }
}