using System.Text.Json;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Application.Features.Outbox.Interfaces;
using Domain.Features.Outbox.Enums;

namespace Worker;

public sealed class OutboxDispatcherWorker : BackgroundService
{
    private const int BatchSize = 10;
    private static readonly TimeSpan EmptyPollDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MessageLease = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<OutboxDispatcherWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxDispatcherWorker(
        ILogger<OutboxDispatcherWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Outbox dispatcher worker started. BatchSize: {BatchSize}", BatchSize);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var emailJobQueue = scope.ServiceProvider.GetRequiredService<IEmailJobQueue>();

                var messages = await outboxRepository.ClaimPendingAsync(BatchSize, MessageLease, ct);
                if (messages.Count == 0)
                {
                    await Task.Delay(EmptyPollDelay, ct);
                    continue;
                }

                foreach (var message in messages)
                {
                    await DispatchMessageAsync(outboxRepository, emailJobQueue, message.Id, message.Type, message.Payload, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch outbox messages.");
                await Task.Delay(FailureDelay, ct);
            }
        }
    }

    private async Task DispatchMessageAsync(
        IOutboxRepository outboxRepository,
        IEmailJobQueue emailJobQueue,
        Guid messageId,
        OutboxMessageType type,
        string payload,
        CancellationToken ct)
    {
        try
        {
            if (type != OutboxMessageType.EmailJob)
            {
                _logger.LogError("Unknown outbox message type {Type}. MessageId: {MessageId}", type, messageId);
                await outboxRepository.MarkFailedAsync(messageId, ct);
                return;
            }

            var emailJob = JsonSerializer.Deserialize<EmailJobMessage>(payload, JsonOptions);
            if (emailJob is null)
            {
                _logger.LogError("Outbox email job payload was empty. MessageId: {MessageId}", messageId);
                await outboxRepository.MarkFailedAsync(messageId, ct);
                return;
            }

            await emailJobQueue.EnqueueAsync(emailJob, ct);
            await outboxRepository.MarkProcessedAsync(messageId, ct);

            _logger.LogInformation(
                "Dispatched outbox email job {Type} for user {UserId}. MessageId: {MessageId}",
                emailJob.Type,
                emailJob.UserId,
                messageId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize outbox message payload. MessageId: {MessageId}", messageId);
            await outboxRepository.MarkFailedAsync(messageId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch outbox message. MessageId: {MessageId}", messageId);
            await outboxRepository.MarkRetryAsync(messageId, FailureDelay, ct);
        }
    }
}
