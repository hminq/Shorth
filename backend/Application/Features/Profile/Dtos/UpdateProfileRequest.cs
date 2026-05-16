namespace Application.Features.Profile.Dtos;

public sealed record UpdateProfileRequest(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? CurrentPassword,
    string? NewPassword);
