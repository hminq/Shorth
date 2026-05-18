using System.Text.Json;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastucture.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private const string KeyPrefix = "auth:password-reset:token:";

    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl;

    public PasswordResetTokenRepository(
        IDistributedCache cache,
        TimeSpan ttl)
    {
        _cache = cache;
        _ttl = ttl;
    }

    public async Task StoreAsync(
        string token,
        PasswordResetTokenState state,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Reset token is required.", nameof(token));
        }

        var cacheKey = BuildKey(token);
        var payload = JsonSerializer.Serialize(state);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttl
        };

        await _cache.SetStringAsync(cacheKey, payload, options, ct);
    }

    public async Task<PasswordResetTokenState?> TakeAsync(
        string token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var cacheKey = BuildKey(token);
        var payload = await _cache.GetStringAsync(cacheKey, ct);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        await _cache.RemoveAsync(cacheKey, ct);
        return JsonSerializer.Deserialize<PasswordResetTokenState>(payload);
    }

    private static string BuildKey(string token)
    {
        return $"{KeyPrefix}{token}";
    }
}
