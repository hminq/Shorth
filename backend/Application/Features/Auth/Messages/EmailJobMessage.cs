namespace Application.Features.Auth.Messages;

public sealed record EmailJobMessage(
    EmailJobType Type,
    Guid UserId,
    string Email,
    string? DisplayName,
    string? OtpCode,
    DateTime EnqueuedAtUtc
);
