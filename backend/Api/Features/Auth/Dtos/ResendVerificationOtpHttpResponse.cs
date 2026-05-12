namespace Api.Features.Auth.Dtos;

public sealed record ResendVerificationOtpHttpResponse(
    string Email,
    bool RequiresEmailVerification
);
