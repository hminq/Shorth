namespace Application.Features.Auth.Dtos;

public sealed record ResendVerificationOtpResult(
    string Email,
    bool RequiresEmailVerification
);
