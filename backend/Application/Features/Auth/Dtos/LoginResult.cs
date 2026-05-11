namespace Application.Features.Auth.Dtos;

public sealed record LoginResult(
    string AccessToken,
    Guid UserId,
    string Email,
    string DisplayName
);
