namespace Application.Features.Links.Messages;

public sealed record ClickEventMessage(
    Guid EventId,
    Guid LinkId,
    DateTime ClickedAt,
    string? UserAgent,
    string? Referrer,
    string? IpHash,
    string? CountryCode);
