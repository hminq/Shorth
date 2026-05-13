using System;
using Application.Features.Links.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastucture.Repositories;

public class LinkCacheRepository : ILinkCacheRepository
{
    private const string KeyPrefix = "link:destination:";

    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cacheDuration;

    public LinkCacheRepository(
        IDistributedCache cache,
        TimeSpan cacheDuration)
    {
        _cache = cache;
        _cacheDuration = cacheDuration;
    }

    public async Task<string?> GetDestinationUrlBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _cache.GetStringAsync(BuildKey(slug), ct);
    }

    public async Task SetDestinationUrlBySlugAsync(string slug, string destinationUrl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration
        };

        await _cache.SetStringAsync(BuildKey(slug), destinationUrl, options, ct);
    }

    private string BuildKey(string slug)
    {
        return $"{KeyPrefix}{slug}";
    }
}
