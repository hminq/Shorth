namespace Application.Features.Links.Dtos;

public sealed record ResolveLinkRequest(
    string Slug,
    string? UserAgent,
    string? Referrer,
    string? IpHash,
    string? CountryCode);
