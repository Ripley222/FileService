using CSharpFunctionalExtensions;
using FileService.Domain.Entities.Enums;
using FileService.Domain.ValueObjects;
using FileService.Domain.ValueObjects.Enums;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.Entities;

public class VideoAsset : MediaAsset
{
    public const long MAX_SIZE = 5_368_709_120; // 5 GB

    public const string BUCKET = "videos";
    public const string RAW_PREFIX = "raw";
    public const string HLS_PREFIX = "hls";
    public const string MASTER_PLAYLIST_NAME = "master.m3u8";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public StorageKey HlsRootKey { get; private set; } = null!;

    //EfCore constructor
    private VideoAsset() 
    {
    }
    
    private VideoAsset(
        Guid id,
        MediaData mediaData,
        AssetType assetType,
        Status status,
        StorageKey rawKey,
        StorageKey finalKey,
        MediaOwner owner) : base(id, mediaData, assetType, status, rawKey, finalKey, owner)
    {
    }

    public static Result<VideoAsset, Error> Create(Guid id, MediaData mediaData, MediaOwner owner)
    {
        var validationResult = ValidateForUpload(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        var rawKeyResult = StorageKey.Create(BUCKET, RAW_PREFIX, id.ToString());
        if (rawKeyResult.IsFailure)
            return rawKeyResult.Error;

        return new VideoAsset(
            id, mediaData, AssetType.Video, Status.Uploading, rawKeyResult.Value, null, owner);
    }

    public UnitResult<Error> CompleteProcessing(DateTime timestamp)
    {
        HlsRootKey = StorageKey.Create(BUCKET, HLS_PREFIX, Id.ToString()).Value;
        
        var finalKeyResult = HlsRootKey.AppendSegment(MASTER_PLAYLIST_NAME);
        if (finalKeyResult.IsFailure)
            return finalKeyResult.Error;

        var markReadyResult = MarkReady(finalKeyResult.Value, timestamp);
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
                $"Тип контента должен быть: {nameof(MediaType.Video)}!");

        if (mediaData.Size > MAX_SIZE)
            return Error.Validation(
                "video.invalid.size",
                $"Максимально допустимый размер видео: {MAX_SIZE}!");

        return UnitResult.Success<Error>();
    }
}