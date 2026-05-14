namespace Application.Features.Links.Dtos;

public sealed record LinkDetailResult(
    Guid Id,
    string Slug,
    string DestinationUrl,
    long ClickCount,
    DateTime? LastClickedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ExpiresAt,
    bool IsDisabled);
