namespace FileService.Contracts.DTOs;

public record FileInfoDto(string FileName, string ContentType, long Size);