namespace Application.Features.Auth.Dtos;

public sealed record LocalLoginRequest(
    string Email,
    string Password
);
