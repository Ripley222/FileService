namespace FileService.Contracts.Responses;

public sealed record MultipartUploadResponse(
    Guid MediaAssetId,
    string UploadId,
    string[] Urls,
    long ChunkSize);