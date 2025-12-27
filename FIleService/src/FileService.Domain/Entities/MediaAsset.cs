using CSharpFunctionalExtensions;
using FileService.Domain.Entities.Enums;
using FileService.Domain.ValueObjects;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.Entities;

public abstract class MediaAsset
{
    public Guid Id { get; protected set; }
    public MediaData MediaData { get; protected set; }
    public AssetType AssetType { get; protected set; }
    public Status Status { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public StorageKey? RawKey { get; protected set; }
    public StorageKey? FinalKey { get; protected set; }
    public MediaOwner Owner { get; protected set; }

    //EfCore constructor
    protected MediaAsset()
    {
    }

    protected MediaAsset(
        Guid id,
        MediaData mediaData,
        AssetType assetType,
        Status status,
        StorageKey? rawKey,
        StorageKey? finalKey,
        MediaOwner owner)
    {
        Id = id;
        MediaData = mediaData;
        AssetType = assetType;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        RawKey = rawKey;
        FinalKey = finalKey;
        Owner = owner;
    }

    public static Result<MediaAsset, Error> CreateForUpload(MediaData mediaData, AssetType assetType)
    {
        var assetId = Guid.NewGuid();
        var mediaOwner = MediaOwner.ForDepartments(Guid.NewGuid()).Value;

        switch (assetType)
        {
            case AssetType.Video:
                var videoResult = VideoAsset.CreateForUpload(assetId, mediaData, mediaOwner);
                return videoResult.IsFailure ? videoResult.Error : videoResult.Value;
            case AssetType.Preview:
                var previewResult = PreviewAsset.CreateForUpload(assetId, mediaData, mediaOwner);
                return previewResult.IsFailure ? previewResult.Error : previewResult.Value;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null);
        }
    }

    public UnitResult<Error> MarkUploaded(DateTime updatedAt)
    {
        if (Status is Status.Uploaded)
            return UnitResult.Success<Error>();

        if (Status is not Status.Uploading)
            return Error.Validation(
                "media.status.invalid",
                $"Статус медиа файла должен быть: {nameof(Status.Uploading)}!");

        Status = Status.Uploaded;
        UpdatedAt = updatedAt;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkReady(StorageKey finalKey, DateTime updatedAt)
    {
        if (Status is Status.Ready)
            return UnitResult.Success<Error>();

        if (Status is not Status.Uploaded)
            return Error.Validation(
                "media.status.invalid",
                $"Статус медиа файла должен быть: {nameof(Status.Uploaded)}!");

        FinalKey = finalKey;
        Status = Status.Ready;
        UpdatedAt = updatedAt;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkFailed(DateTime updatedAt)
    {
        if (Status is Status.Failed)
            return UnitResult.Success<Error>();

        if (Status is not (Status.Uploading or Status.Uploaded))
            return Error.Validation(
                "media.status.invalid",
                $"Статус медиа файла должен быть: {nameof(Status.Uploading)} или {nameof(Status.Uploaded)}!");

        Status = Status.Failed;
        UpdatedAt = updatedAt;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkDeleted(DateTime updatedAt)
    {
        if (Status is Status.Deleted)
            return UnitResult.Success<Error>();

        Status = Status.Deleted;
        UpdatedAt = updatedAt;

        return UnitResult.Success<Error>();
    }
}