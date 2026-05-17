using System;
using System.Text.Json;
using Application.Features.Links.Dtos;
using Application.Features.Links.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastucture.Repositories;

public class LinkCacheRepository : ILinkCacheRepository
{
    private const string KeyPrefix = "link:destination:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cacheDuration;

    public LinkCacheRepository(
        IDistributedCache cache,
        TimeSpan cacheDuration)
    {
        _cache = cache;
        _cacheDuration = cacheDuration;
    }

    public async Task<LinkCacheEntry?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var cached = await _cache.GetStringAsync(BuildKey(slug), ct);
        if (string.IsNullOrWhiteSpace(cached))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LinkCacheEntry>(cached, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SetBySlugAsync(string slug, LinkCacheEntry entry, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration
        };

        await _cache.SetStringAsync(BuildKey(slug), JsonSerializer.Serialize(entry, JsonOptions), options, ct);
    }

    private string BuildKey(string slug)
    {
        return $"{KeyPrefix}{slug}";
    }
}
