using CSharpFunctionalExtensions;
using FileService.Domain.Entities.Enums;
using FileService.Domain.ValueObjects;
using FileService.Domain.ValueObjects.Enums;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.Entities;

public class PreviewAsset : MediaAsset
{
    public const long MAX_SIZE = 10_485_760; // 10 MB

    public const string BUCKET = "preview";
    public const string RAW_PREFIX = "raw";

    public static readonly string[] AllowedExtensions = ["jpg", "jpeg", "png", "webp"];

    //EfCore constructor
    private PreviewAsset() 
    {
    }
    
    private PreviewAsset(
        Guid id,
        MediaData mediaData,
        AssetType assetType,
        Status status,
        StorageKey rawKey,
        StorageKey finalKey,
        MediaOwner owner) : base(id, mediaData, assetType, status, rawKey, finalKey, owner)
    {
    }

    public static Result<PreviewAsset, Error> Create(Guid id, MediaData mediaData, MediaOwner owner)
    {
        var validationResult = ValidateForUpload(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        var keyResult = StorageKey.Create(BUCKET, RAW_PREFIX, id.ToString());
        if (keyResult.IsFailure)
            return keyResult.Error;

        return new PreviewAsset(
            id, mediaData, AssetType.Preview, Status.Uploading, keyResult.Value, null, owner);
    }

    public UnitResult<Error> CompleteUpload(DateTime timestamp)
    {
        var markUploadedResult = MarkUploaded(timestamp);
        if (markUploadedResult.IsFailure)
            return markUploadedResult.Error;

        var markReadyResult = MarkReady(RawKey, timestamp);
        if (markReadyResult.IsFailure)
            return markReadyResult.Error;

        return UnitResult.Success<Error>();
    }

    private static UnitResult<Error> ValidateForUpload(MediaData mediaData)
    {
        if (AllowedExtensions.Contains(mediaData.FileName.Extension) is false)
            return Error.Validation(
                "video.invalid.extension",
                $"Допустимые расширения: {string.Join(", ", AllowedExtensions)}!");

        if (mediaData.ContentType.Category is not MediaType.Video)
            return Error.Validation(
                "video.invalid.content-type",
                $"Тип контента должен быть: {nameof(MediaType.Image)}!");

        if (mediaData.Size > MAX_SIZE)
            return Error.Validation(
                "video.invalid.size",
                $"Максимально допустимый размер изображения: {MAX_SIZE}!");

        return UnitResult.Success<Error>();
    }
}