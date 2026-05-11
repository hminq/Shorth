using System;

namespace Application.Features.Links.Interfaces;

public interface IClickEventQueue
{
    Task EnqueueAsync(Guid linkId, DateTime clickTime, CancellationToken ct = default);
}
