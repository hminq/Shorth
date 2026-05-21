namespace Api.Configurations;

public sealed record AuthCookieOptions(
    string CookieName,
    string RefreshCookieName,
    string WebBaseUrl,
    int AccessTokenTtlMinutes,
    int RefreshTokenTtlDays);
