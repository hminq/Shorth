namespace Application.Dtos;

public sealed record LocalLoginRequest(
    string Email,
    string Password
);
