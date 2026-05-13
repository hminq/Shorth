using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Links.Interfaces;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class SqsClickEventQueue : IClickEventQueue
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsClickEventQueue(IAmazonSQS sqsClient, SqsOptions options)
    {
        _sqsClient = sqsClient;
        _queueUrl = options.ClickEventsQueueUrl;
    }

    public async Task EnqueueAsync(Guid linkId, DateTime clickTime, CancellationToken ct = default)
    {
        var message = new ClickEventQueueMessage(linkId, clickTime);

        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonSerializer.Serialize(message)
        };

        await _sqsClient.SendMessageAsync(request, ct);
    }

    private sealed record ClickEventQueueMessage(
        Guid LinkId,
        DateTime ClickTime);
}
