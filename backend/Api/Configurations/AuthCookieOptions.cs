namespace Api.Configurations;

public sealed record AuthCookieOptions(
    string CookieName,
    string WebBaseUrl,
    int AccessTokenTtlMinutes);
