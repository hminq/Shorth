namespace Infrastucture.Configurations;

public sealed record GoogleAuthOptions(
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    int StateTtlMinutes);
