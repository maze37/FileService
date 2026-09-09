using FileService.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileService.Infrastructure.S3;

public class S3HealthCheck : IHealthCheck
{
    private readonly IS3Provider _s3Provider;

    public S3HealthCheck(IS3Provider s3Provider)
    {
        _s3Provider = s3Provider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool isAvailable = await _s3Provider.IsAvailableAsync(cancellationToken);

        return isAvailable
            ? HealthCheckResult.Healthy("S3 storage доступен")
            : HealthCheckResult.Unhealthy("S3 storage недоступен");
    }
}