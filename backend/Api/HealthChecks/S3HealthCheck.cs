using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

public sealed class S3HealthCheck(
    IAmazonS3 s3Client,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var bucketName = configuration["S3_BUCKET_NAME"];
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return HealthCheckResult.Unhealthy("S3 bucket name is not configured.");
        }

        try
        {
            await s3Client.HeadBucketAsync(
                new HeadBucketRequest
                {
                    BucketName = bucketName
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
