using Application.Features.Outbox.Interfaces;
using Domain.Features.Outbox.Entities;
using Domain.Features.Outbox.Enums;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _dbContext;

    public OutboxRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int batchSize,
        TimeSpan lease,
        CancellationToken ct = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        var messages = await _dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM outbox_messages
                WHERE (
                    status = 'pending'
                    AND next_attempt_at <= {now}
                )
                OR (
                    status = 'processing'
                    AND locked_until IS NOT NULL
                    AND locked_until <= {now}
                )
                ORDER BY created_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            message.MarkProcessing(now, lease);
        }

        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return messages;
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await GetMessageAsync(messageId, ct);
        message.MarkProcessed(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkRetryAsync(Guid messageId, TimeSpan delay, CancellationToken ct = default)
    {
        var message = await GetMessageAsync(messageId, ct);
        var now = DateTime.UtcNow;
        message.MarkRetry(now, now.Add(delay));

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await GetMessageAsync(messageId, ct);
        message.MarkFailed(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<OutboxMessage> GetMessageAsync(Guid messageId, CancellationToken ct)
    {
        var message = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(x => x.Id == messageId, ct);

        if (message is null)
        {
            throw new InvalidOperationException("Outbox message was not found.");
        }

        if (message.Status is OutboxMessageStatus.Processed or OutboxMessageStatus.Failed)
        {
            throw new InvalidOperationException("Outbox message is already complete.");
        }

        return message;
    }
}
