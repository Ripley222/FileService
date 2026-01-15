namespace FileService.Contracts.Requests;

public sealed record DownloadPresignedUrlRequest(Guid FileId);