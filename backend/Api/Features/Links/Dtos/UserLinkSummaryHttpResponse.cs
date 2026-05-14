namespace Api.Features.Links.Dtos;

public sealed record UserLinkSummaryHttpResponse(
    Guid Id,
    string Slug,
    string DestinationUrl,
    long ClickCount,
    DateTime? LastClickedAt,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsDisabled);
