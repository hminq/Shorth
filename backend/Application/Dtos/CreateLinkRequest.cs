using System;

namespace Application.Dtos;

public sealed record CreateLinkRequest(
    Guid? OwnerId,
    string DestinationUrl,
    DateTime? ExpiresAt
);
