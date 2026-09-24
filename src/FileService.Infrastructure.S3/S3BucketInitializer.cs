using FileService.Core;
using FileService.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public class S3BucketInitializer : IHostedService
{
    private readonly IS3Provider _s3Provider;
    private readonly FileStorageOptions _options;
    private readonly ILogger<S3BucketInitializer> _logger;

    public S3BucketInitializer(
        IS3Provider s3Provider,
        IOptions<FileStorageOptions> options,
        ILogger<S3BucketInitializer> logger)
    {
        _s3Provider = s3Provider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        const int delaySeconds = 3;

        foreach (string bucket in _options.RequiredBuckets)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var result = await _s3Provider.EnsureBucketExistsAsync(bucket, cancellationToken);

                if (result.IsSuccess)
                    break;

                if (attempt == maxAttempts)
                {
                    _logger.LogError(
                        "Failed to init bucket {Bucket} after {Attempts} attempts: {Error}",
                        bucket, maxAttempts, result.Error);
                    throw new InvalidOperationException(
                        $"Не удалось инициализировать bucket '{bucket}' после {maxAttempts} попыток: {result.Error}");
                }

                _logger.LogWarning(
                    "Attempt {Attempt}/{Max} to init bucket {Bucket} failed: {Error}. Retrying in {Delay}s",
                    attempt, maxAttempts, bucket, result.Error, delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}