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
        var emailJobsQueueUrl = configuration["Sqs:EmailJobsQueueUrl"];
        if (string.IsNullOrWhiteSpace(emailJobsQueueUrl))
        {
            return HealthCheckResult.Unhealthy("SQS email jobs queue url is not configured.");
        }

        var clickEventsQueueUrl = configuration["Sqs:ClickEventsQueueUrl"];
        if (string.IsNullOrWhiteSpace(clickEventsQueueUrl))
        {
            return HealthCheckResult.Unhealthy("SQS click events queue url is not configured.");
        }

        try
        {
            await sqsClient.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = emailJobsQueueUrl,
                    AttributeNames = ["QueueArn"]
                },
                cancellationToken);

            await sqsClient.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = clickEventsQueueUrl,
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
