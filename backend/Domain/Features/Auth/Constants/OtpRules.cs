namespace Domain.Features.Auth.Constants;

public static class OtpRules
{
    public const int CodeLength = 6;
    public const int MaxAttempts = 3;
    public static readonly TimeSpan EmailVerificationTtl = TimeSpan.FromMinutes(10);
}
