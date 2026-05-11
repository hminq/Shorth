using System;

namespace Application.Features.Links.Interfaces;

public interface ILinkCacheRepository
{
    Task<string?> GetDestinationUrlBySlugAsync(string slug, CancellationToken ct = default);
    Task SetDestinationUrlBySlugAsync(string slug, string destinationUrl, CancellationToken ct = default);
}
