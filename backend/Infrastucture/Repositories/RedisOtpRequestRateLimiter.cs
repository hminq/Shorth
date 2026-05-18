using Application.Features.Auth.Interfaces;
using StackExchange.Redis;

namespace Infrastucture.Repositories;

public sealed class RedisOtpRequestRateLimiter : IOtpRequestRateLimiter
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisOtpRequestRateLimiter(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<bool> TryConsumeAsync(
        string key,
        int maxRequests,
        TimeSpan window,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Rate limit key is required.", nameof(key));
        }

        if (maxRequests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRequests), "Max requests must be positive.");
        }

        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Rate limit window must be positive.");
        }

        ct.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        var count = await database.StringIncrementAsync(key);

        if (count == 1)
        {
            await database.KeyExpireAsync(key, window);
        }

        return count <= maxRequests;
    }
}
