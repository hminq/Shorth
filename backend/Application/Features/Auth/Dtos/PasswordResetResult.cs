namespace Application.Features.Auth.Dtos;

public sealed record PasswordResetResult(
    string Email,
    bool PasswordReset
);
