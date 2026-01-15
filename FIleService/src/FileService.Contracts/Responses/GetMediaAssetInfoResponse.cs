using FileService.Contracts.DTOs;

namespace FileService.Contracts.Responses;

public sealed record GetMediaAssetInfoResponse(MediaAssetDto MediaAssetDto);