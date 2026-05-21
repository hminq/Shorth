using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Links.Messages;
using Infrastucture.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastucture.Repositories;

public sealed class SqsClickEventPublisher : BackgroundService
{
    private const int MaxBatchSize = 10;
    private static readonly TimeSpan MaxBatchDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan PublishFailureDelay = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ClickEventChannel _channel;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<SqsClickEventPublisher> _logger;

    public SqsClickEventPublisher(
        ClickEventChannel channel,
        IAmazonSQS sqsClient,
        SqsOptions options,
        ILogger<SqsClickEventPublisher> logger)
    {
        _channel = channel;
        _sqsClient = sqsClient;
        _queueUrl = options.ClickEventsQueueUrl;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "SQS click event publisher started. QueueUrl: {QueueUrl}, ChannelCapacity: {ChannelCapacity}, MaxBatchSize: {MaxBatchSize}",
            _queueUrl,
            ClickEventChannel.Capacity,
            MaxBatchSize);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var firstMessage = await _channel.Reader.ReadAsync(ct);
                var batch = new List<ClickEventMessage>(MaxBatchSize) { firstMessage };

                await FillBatchAsync(batch, ct);
                await PublishBatchAsync(batch, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish click event batch to SQS.");
                await Task.Delay(PublishFailureDelay, ct);
            }
        }
    }

    private async Task FillBatchAsync(List<ClickEventMessage> batch, CancellationToken ct)
    {
        var flushAt = DateTime.UtcNow.Add(MaxBatchDelay);

        while (batch.Count < MaxBatchSize)
        {
            while (batch.Count < MaxBatchSize && _channel.Reader.TryRead(out var message))
            {
                batch.Add(message);
            }

            if (batch.Count >= MaxBatchSize)
            {
                return;
            }

            var remaining = flushAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            var hasMore = _channel.Reader.WaitToReadAsync(ct).AsTask();
            var completed = await Task.WhenAny(hasMore, Task.Delay(remaining, ct));
            if (completed != hasMore)
            {
                return;
            }

            if (!await hasMore)
            {
                return;
            }
        }
    }

    private async Task PublishBatchAsync(IReadOnlyList<ClickEventMessage> batch, CancellationToken ct)
    {
        var entries = batch
            .Select((message, index) => new SendMessageBatchRequestEntry
            {
                Id = $"click-{index}",
                MessageBody = JsonSerializer.Serialize(message, JsonOptions)
            })
            .ToList();

        var response = await _sqsClient.SendMessageBatchAsync(
            new SendMessageBatchRequest
            {
                QueueUrl = _queueUrl,
                Entries = entries
            },
            ct);

        if (response.Failed.Count > 0)
        {
            _logger.LogError(
                "SQS click event batch had {FailedCount} failed entries out of {TotalCount}.",
                response.Failed.Count,
                batch.Count);
        }
    }
}
