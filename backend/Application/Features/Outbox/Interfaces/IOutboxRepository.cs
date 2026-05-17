using Domain.Features.Outbox.Entities;

namespace Application.Features.Outbox.Interfaces;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int batchSize,
        TimeSpan lease,
        CancellationToken ct = default);

    Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default);

    Task MarkRetryAsync(Guid messageId, TimeSpan delay, CancellationToken ct = default);

    Task MarkFailedAsync(Guid messageId, CancellationToken ct = default);
}
