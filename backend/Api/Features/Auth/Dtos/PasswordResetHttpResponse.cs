namespace Api.Features.Auth.Dtos;

public sealed record PasswordResetHttpResponse(
    string Email,
    bool PasswordReset);
