using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

public sealed class SqsHealthCheck(
    IAmazonSQS sqsClient,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var queueUrl = configuration["Sqs:QueueUrl"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return HealthCheckResult.Unhealthy("SQS queue url is not configured.");
        }

        try
        {
            await sqsClient.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = ["QueueArn"]
                },
                cancellationToken);

            return HealthCheckResult.Healthy("SQS is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQS health check failed.", ex);
        }
    }
}
