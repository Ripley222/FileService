namespace FileService.Contracts.Requests;

public sealed record MultipartUploadRequest(
    string FileName,
    string ContentType,
    long Size,
    string Context,
    Guid ContextId);