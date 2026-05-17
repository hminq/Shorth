using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Links.Interfaces;
using Application.Features.Links.Messages;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class SqsClickEventQueue : IClickEventQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsClickEventQueue(IAmazonSQS sqsClient, SqsOptions options)
    {
        _sqsClient = sqsClient;
        _queueUrl = options.ClickEventsQueueUrl;
    }

    public async Task EnqueueAsync(
        Guid linkId,
        DateTime clickedAt,
        string? userAgent,
        string? referrer,
        string? ipHash,
        string? countryCode,
        CancellationToken ct = default)
    {
        var message = new ClickEventMessage(
            Guid.NewGuid(),
            linkId,
            clickedAt,
            userAgent,
            referrer,
            ipHash,
            countryCode);

        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonSerializer.Serialize(message, JsonOptions)
        };

        await _sqsClient.SendMessageAsync(request, ct);
    }
}
