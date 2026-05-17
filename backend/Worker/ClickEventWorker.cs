using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Links.Interfaces;
using Application.Features.Links.Messages;
using Domain.Features.Links.Entities;
using Infrastucture.Configurations;

namespace Worker;

public sealed class ClickEventWorker : BackgroundService
{
    private const int MaxMessagesPerPoll = 10;
    private const int LongPollSeconds = 20;
    private static readonly TimeSpan PollFailureDelay = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<ClickEventWorker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _queueUrl;

    public ClickEventWorker(
        ILogger<ClickEventWorker> logger,
        IAmazonSQS sqsClient,
        IServiceScopeFactory scopeFactory,
        SqsOptions options)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _scopeFactory = scopeFactory;
        _queueUrl = options.ClickEventsQueueUrl;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Click event worker started. QueueUrl: {QueueUrl}, MaxMessagesPerPoll: {MaxMessagesPerPoll}, LongPollSeconds: {LongPollSeconds}",
            _queueUrl,
            MaxMessagesPerPoll,
            LongPollSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(
                    new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = MaxMessagesPerPoll,
                        WaitTimeSeconds = LongPollSeconds
                    },
                    ct);

                if (response.Messages is null || response.Messages.Count == 0)
                {
                    continue;
                }

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to poll click events queue.");
                await Task.Delay(PollFailureDelay, ct);
            }
        }
    }

    private async Task ProcessMessageAsync(Message sqsMessage, CancellationToken ct)
    {
        ClickEventMessage? clickEventMessage;

        try
        {
            clickEventMessage = JsonSerializer.Deserialize<ClickEventMessage>(sqsMessage.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize click event message. MessageId: {MessageId}", sqsMessage.MessageId);
            await DeleteMessageAsync(sqsMessage, ct);
            return;
        }

        if (clickEventMessage is null || clickEventMessage.EventId == Guid.Empty || clickEventMessage.LinkId == Guid.Empty)
        {
            _logger.LogWarning("Click event message body was invalid. MessageId: {MessageId}", sqsMessage.MessageId);
            await DeleteMessageAsync(sqsMessage, ct);
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ILinkClickEventRepository>();
            var clickEvent = LinkClickEvent.Create(
                clickEventMessage.EventId,
                clickEventMessage.LinkId,
                clickEventMessage.ClickedAt,
                clickEventMessage.UserAgent,
                clickEventMessage.Referrer,
                clickEventMessage.IpHash,
                clickEventMessage.CountryCode,
                null,
                null,
                null);

            await repository.SaveClickAndUpdateAnalyticsAsync(clickEvent, ct);
            await DeleteMessageAsync(sqsMessage, ct);

            _logger.LogInformation(
                "Processed click event {EventId} for link {LinkId}. MessageId: {MessageId}",
                clickEventMessage.EventId,
                clickEventMessage.LinkId,
                sqsMessage.MessageId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Click event payload was invalid. MessageId: {MessageId}", sqsMessage.MessageId);
            await DeleteMessageAsync(sqsMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process click event {EventId} for link {LinkId}. MessageId: {MessageId}",
                clickEventMessage.EventId,
                clickEventMessage.LinkId,
                sqsMessage.MessageId);
        }
    }

    private async Task DeleteMessageAsync(Message sqsMessage, CancellationToken ct)
    {
        await _sqsClient.DeleteMessageAsync(
            new DeleteMessageRequest
            {
                QueueUrl = _queueUrl,
                ReceiptHandle = sqsMessage.ReceiptHandle
            },
            ct);
    }
}
