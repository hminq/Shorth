using System.Text.Json;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastucture.Repositories;

public sealed class GoogleAuthStateRepository : IGoogleAuthStateRepository
{
    private const string KeyPrefix = "auth:google:state:";

    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl;

    public GoogleAuthStateRepository(
        IDistributedCache cache,
        TimeSpan ttl)
    {
        _cache = cache;
        _ttl = ttl;
    }

    public async Task StoreAsync(
        string state,
        GoogleAuthState authState,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State is required.", nameof(state));
        }

        var cacheKey = BuildKey(state);
        var payload = JsonSerializer.Serialize(authState);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttl
        };

        await _cache.SetStringAsync(cacheKey, payload, options, ct);
    }

    public async Task<GoogleAuthState?> TakeAsync(
        string state,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        var cacheKey = BuildKey(state);
        var payload = await _cache.GetStringAsync(cacheKey, ct);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        await _cache.RemoveAsync(cacheKey, ct);
        return JsonSerializer.Deserialize<GoogleAuthState>(payload);
    }

    private string BuildKey(string state)
    {
        return $"{KeyPrefix}{state}";
    }
}
