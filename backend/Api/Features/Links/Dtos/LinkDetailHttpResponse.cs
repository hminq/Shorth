namespace Api.Features.Links.Dtos;

public sealed record LinkDetailHttpResponse(
    Guid Id,
    string Slug,
    string DestinationUrl,
    long ClickCount,
    DateTime? LastClickedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ExpiresAt,
    bool IsDisabled);
