namespace Application.Features.Auth.Dtos;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string DisplayName
);
