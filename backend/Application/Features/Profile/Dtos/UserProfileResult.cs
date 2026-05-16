namespace Application.Features.Profile.Dtos;

public sealed record UserProfileResult(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool HasPassword);
