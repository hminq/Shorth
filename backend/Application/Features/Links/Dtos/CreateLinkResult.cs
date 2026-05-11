using System;

namespace Application.Features.Links.Dtos;

public sealed record CreateLinkResult(
    Guid Id,
    Guid? OwnerId,
    string Slug,
    string DestinationUrl,
    DateTime CreatedAt,
    DateTime? ExpiresAt
);
