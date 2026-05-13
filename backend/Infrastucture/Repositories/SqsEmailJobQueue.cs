using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class SqsEmailJobQueue : IEmailJobQueue
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsEmailJobQueue(IAmazonSQS sqsClient, SqsOptions options)
    {
        _sqsClient = sqsClient;
        _queueUrl = options.EmailJobsQueueUrl;
    }

    public async Task EnqueueAsync(EmailJobMessage message, CancellationToken ct = default)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonSerializer.Serialize(message)
        };

        await _sqsClient.SendMessageAsync(request, ct);
    }
}
