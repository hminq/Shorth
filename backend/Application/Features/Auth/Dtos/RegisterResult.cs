namespace Application.Features.Auth.Dtos;

public sealed record RegisterResult(
    Guid UserId,
    string Email,
    string DisplayName,
    bool RequiresEmailVerification
);
