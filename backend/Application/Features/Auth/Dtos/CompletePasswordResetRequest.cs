namespace Application.Features.Auth.Dtos;

public sealed record CompletePasswordResetRequest(
    string ResetToken,
    string NewPassword
);
