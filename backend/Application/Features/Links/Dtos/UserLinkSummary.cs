namespace Application.Features.Links.Dtos;

public sealed record UserLinkSummary(
    Guid Id,
    string Slug,
    string DestinationUrl,
    long ClickCount,
    DateTime? LastClickedAt,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsDisabled);
