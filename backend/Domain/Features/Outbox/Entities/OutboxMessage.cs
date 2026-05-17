using Domain.Features.Outbox.Constants;
using Domain.Features.Outbox.Enums;

namespace Domain.Features.Outbox.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public OutboxMessageType Type { get; private set; }
    public string Payload { get; private set; } = null!;
    public OutboxMessageStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(OutboxMessageType type, string payload, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Outbox message payload is required.", nameof(payload));
        }

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0,
            NextAttemptAt = createdAt,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void MarkProcessing(DateTime now, TimeSpan lease)
    {
        Status = OutboxMessageStatus.Processing;
        LockedUntil = now.Add(lease);
        UpdatedAt = now;
    }

    public void MarkProcessed(DateTime processedAt)
    {
        Status = OutboxMessageStatus.Processed;
        LockedUntil = null;
        ProcessedAt = processedAt;
        UpdatedAt = processedAt;
    }

    public void MarkRetry(DateTime now, DateTime nextAttemptAt)
    {
        if (RetryCount + 1 >= OutboxRules.MaxRetryCount)
        {
            MarkFailed(now);
            return;
        }

        Status = OutboxMessageStatus.Pending;
        RetryCount++;
        LockedUntil = null;
        NextAttemptAt = nextAttemptAt;
        UpdatedAt = now;
    }

    public void MarkFailed(DateTime failedAt)
    {
        Status = OutboxMessageStatus.Failed;
        LockedUntil = null;
        UpdatedAt = failedAt;
    }
}
