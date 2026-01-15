namespace FileService.Contracts.Requests;

public sealed record ChunkUploadUrlRequest(Guid MediaAssetId, string UploadId, int PartNumber);