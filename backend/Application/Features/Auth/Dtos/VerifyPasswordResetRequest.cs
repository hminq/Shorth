namespace Application.Features.Auth.Dtos;

public sealed record VerifyPasswordResetRequest(
    string Email,
    string OtpCode
);
