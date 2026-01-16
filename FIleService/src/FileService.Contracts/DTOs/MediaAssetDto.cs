namespace FileService.Contracts.DTOs;

public record MediaAssetDto(
    Guid MediaAssetId,
    string Status,
    string AssetType,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    FileInfoDto FileInfo,
    string DownloadUrl);