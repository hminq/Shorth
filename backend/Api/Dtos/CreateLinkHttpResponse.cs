namespace Api.Dtos;

public sealed record CreateLinkHttpResponse(
    Guid Id,
    string Slug,
    string DestinationUrl,
    DateTime CreatedAt,
    DateTime? ExpiresAt
);
