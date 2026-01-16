using FileService.Contracts.DTOs;

namespace FileService.Contracts.Responses;

public sealed record GetMediaAssetsInfoResponse(IReadOnlyList<MediaAssetDto> MediaAssets);