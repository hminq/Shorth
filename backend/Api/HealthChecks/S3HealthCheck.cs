using Amazon.S3;
using Amazon.S3.Model;
using Infrastucture.Configurations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

public sealed class S3HealthCheck(
    IAmazonS3 s3Client,
    S3Options options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.HeadBucketAsync(
                new HeadBucketRequest
                {
                    BucketName = options.BucketName
                },
                cancellationToken);

            return HealthCheckResult.Healthy("S3 is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 health check failed.", ex);
        }
    }
}
