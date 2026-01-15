namespace FileService.Core.FileProviders;

public interface IS3Options
{
    public string Endpoint { get; init; }

    public string AccessKey { get; init; }

    public string SecretKey { get; init; }

    public bool WithSsl { get; init; }

    public IReadOnlyList<string> RequiredBuckets { get; init; }

    public int DownloadUrlExpirationHours { get; init; }

    public int UploadUrlExpirationMinutes { get; init; }

    public int MaxConcurrentRequests { get; init; }

    public int RecommendedChunksSizeBytes { get; init; }

    public int MaxChunks { get; init; }
}