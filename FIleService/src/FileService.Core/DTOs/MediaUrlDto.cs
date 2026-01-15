using FileService.Domain.ValueObjects;

namespace FileService.Core.DTOs;

public record MediaUrlDto(StorageKey StorageKey, string DownloadUrl);