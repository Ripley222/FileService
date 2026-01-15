namespace FileService.Contracts.Responses;

public sealed record ChunkUploadUrlResponse(string UploadUrl, int PartNumber);