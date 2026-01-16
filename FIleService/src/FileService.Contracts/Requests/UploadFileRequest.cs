namespace FileService.Contracts.Requests;

public sealed record UploadFileRequest(string FileName, Stream Stream, string ContentType, long Size);