namespace Application.Features.Auth.Dtos;

public sealed record VerifyEmailOtpResult(
    Guid UserId,
    string Email,
    bool EmailVerified
);
