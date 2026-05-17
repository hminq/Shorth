using System;

namespace Application.Features.Links.Interfaces;

public interface IClickEventQueue
{
    Task EnqueueAsync(
        Guid linkId,
        DateTime clickedAt,
        string? userAgent,
        string? referrer,
        string? ipHash,
        string? countryCode,
        CancellationToken ct = default);
}
