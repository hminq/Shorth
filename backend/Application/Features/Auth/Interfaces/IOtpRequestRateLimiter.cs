namespace Application.Features.Auth.Interfaces;

public interface IOtpRequestRateLimiter
{
    Task<bool> TryConsumeAsync(
        string key,
        int maxRequests,
        TimeSpan window,
        CancellationToken ct = default);
}
