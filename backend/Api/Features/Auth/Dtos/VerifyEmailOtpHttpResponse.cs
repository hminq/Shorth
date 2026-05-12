namespace Api.Features.Auth.Dtos;

public sealed record VerifyEmailOtpHttpResponse(
    Guid UserId,
    string Email,
    bool EmailVerified
);
