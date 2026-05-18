namespace Application.Features.Auth.Dtos;

public sealed record PasswordResetTokenState(
    Guid UserId,
    string Email,
    DateTime CreatedAtUtc);
