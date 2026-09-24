namespace FileService.Infrastructure.S3;

public record FileStorageOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string ExternalEndpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool WithSsl { get; init; }
    public int DownloadUrlExpirationHours { get; init; } = 24;
    public IReadOnlyList<string> RequiredBuckets { get; init; } = [];
    public double UploadUrlExpirationHours { get; init; } = 1;
    public int MaxConcurrentRequests { get; init; } = 50;
    public long RecommendedChunkSizeBytes { get; init; } = 100 * 1024 * 1024; // 100mb
    public int MaxChunks { get; init; } = 100;
}