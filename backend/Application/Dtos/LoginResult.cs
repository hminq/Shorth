namespace Application.Dtos;

public sealed record LoginResult(
    string AccessToken,
    Guid UserId,
    string Email,
    string DisplayName
);
