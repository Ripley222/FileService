namespace FileService.Contracts.Requests;

public sealed record DownloadFileRequest(Guid FileId, string Path);