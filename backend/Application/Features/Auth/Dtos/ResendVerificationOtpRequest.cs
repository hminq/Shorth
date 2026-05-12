namespace Application.Features.Auth.Dtos;

public sealed record ResendVerificationOtpRequest(
    string Email
);
