namespace FileService.Contracts.Requests;

public sealed record GetMediaAssetsInfoRequest(IReadOnlyList<Guid> MediaAssetIds);