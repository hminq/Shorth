namespace Application.Features.Auth.Dtos;

public sealed record VerifyEmailOtpRequest(
    string Email,
    string OtpCode
);
