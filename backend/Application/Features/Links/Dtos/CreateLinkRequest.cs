using System;

namespace Application.Features.Links.Dtos;

public sealed record CreateLinkRequest(
    Guid? OwnerId,
    string DestinationUrl,
    DateTime? ExpiresAt,
    string? CaptchaToken
);
