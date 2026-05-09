using Microsoft.Extensions.Diagnostics.HealthChecks;
using Resend;

namespace Api.HealthChecks;

public sealed class ResendHealthCheck(IResend resend) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await resend.DomainListAsync(cancellationToken);
            return HealthCheckResult.Healthy("Resend API is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Resend health check failed.", ex);
        }
    }
}
