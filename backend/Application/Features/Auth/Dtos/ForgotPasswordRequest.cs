namespace Application.Features.Auth.Dtos;

public sealed record ForgotPasswordRequest(
    string Email,
    string? ClientIp
);
