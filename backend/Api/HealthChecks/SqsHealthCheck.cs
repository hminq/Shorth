using Amazon.SQS;
using Amazon.SQS.Model;
using Infrastucture.Configurations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

public sealed class SqsHealthCheck(
    IAmazonSQS sqsClient,
    SqsOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await sqsClient.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = options.EmailJobsQueueUrl,
                    AttributeNames = ["QueueArn"]
                },
                cancellationToken);

            await sqsClient.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = options.ClickEventsQueueUrl,
                    AttributeNames = ["QueueArn"]
                },
                cancellationToken);

            return HealthCheckResult.Healthy("SQS queues are reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQS health check failed.", ex);
        }
    }
}
