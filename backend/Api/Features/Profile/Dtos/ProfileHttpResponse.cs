namespace Api.Features.Profile.Dtos;

public sealed record ProfileHttpResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool HasPassword);
