using FileService.Contracts.DTOs;

namespace FileService.Contracts.Requests;

public sealed record CompleteMultipartUploadRequest(
    Guid MediaAssetId,
    string UploadId,
    IReadOnlyList<PartETagDto> PartETags);