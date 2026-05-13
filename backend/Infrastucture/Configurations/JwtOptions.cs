namespace Infrastucture.Configurations;

public sealed record JwtOptions(
    string SigningKey,
    int AccessTokenTtlMinutes);
