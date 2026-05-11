namespace Application.Features.Auth.Dtos;

public sealed record JwtTokenPayload(
    Guid UserId,
    string? Email,
    string? DisplayName
);
