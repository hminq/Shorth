namespace Application.Dtos;

public sealed record JwtTokenPayload(
    Guid UserId,
    string? Email,
    string? DisplayName
);
