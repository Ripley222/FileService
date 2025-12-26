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

    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    //EfCore constructor
    private PreviewAsset()
    {
    }

    private PreviewAsset(
        Guid id,
        MediaData mediaData,
        AssetType assetType,
        Status status,
        StorageKey finalKey,
        MediaOwner owner) : base(id, mediaData, assetType, status, null, finalKey, owner)
    {
    }

    public static Result<PreviewAsset, Error> CreateForUpload(Guid id, MediaData mediaData, MediaOwner owner)
    {
        var validationResult = ValidateForUpload(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        var finalKeyResult = StorageKey.Create(BUCKET, null, id.ToString());
        if (finalKeyResult.IsFailure)
            return finalKeyResult.Error;

        return new PreviewAsset(
            id, mediaData, AssetType.Preview, Status.Ready, finalKeyResult.Value, owner);
    }

    private static UnitResult<Error> ValidateForUpload(MediaData mediaData)
    {
        if (AllowedExtensions.Contains(mediaData.FileName.Extension) is false)
            return Error.Validation(
                "preview.invalid.extension",
                $"Допустимые расширения: {string.Join(", ", AllowedExtensions)}!");

        if (mediaData.ContentType.Category is not MediaType.Image)
            return Error.Validation(
                "preview.invalid.content-type",
                $"Тип контента должен быть: {nameof(MediaType.Image)}!");

        if (mediaData.Size > MAX_SIZE)
            return Error.Validation(
                "preview.invalid.size",
                $"Максимально допустимый размер изображения: {MAX_SIZE}!");

        return UnitResult.Success<Error>();
    }
}