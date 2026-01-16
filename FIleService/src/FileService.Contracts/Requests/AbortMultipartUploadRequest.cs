namespace FileService.Contracts.Requests;

public sealed record AbortMultipartUploadRequest(Guid MediaAssetId, string UploadId);