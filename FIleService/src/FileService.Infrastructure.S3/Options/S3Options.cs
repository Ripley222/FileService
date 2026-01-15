using FileService.Core.FileProviders;

namespace FileService.Infrastructure.S3.Options;

public record S3Options : IS3Options
{
    public string Endpoint { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public bool WithSsl { get; init; }

    public IReadOnlyList<string> RequiredBuckets { get; init; } = [];

    public int DownloadUrlExpirationHours { get; init; } = 24;

    public int UploadUrlExpirationMinutes { get; init; } = 60;

    public int MaxConcurrentRequests { get; init; } = 50;

    public int RecommendedChunksSizeBytes { get; init; } = 10000000;

    public int MaxChunks { get; init; } = 10000;
}