namespace Application.Features.Auth.Dtos;

public sealed record VerifyPasswordResetResult(
    string Email,
    string ResetToken
);
