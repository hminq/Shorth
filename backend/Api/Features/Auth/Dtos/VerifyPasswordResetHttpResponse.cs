namespace Api.Features.Auth.Dtos;

public sealed record VerifyPasswordResetHttpResponse(
    string Email,
    string ResetToken);
