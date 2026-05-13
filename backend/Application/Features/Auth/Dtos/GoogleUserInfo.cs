namespace Application.Features.Auth.Dtos;

public sealed record GoogleUserInfo(
    string ProviderUserId,
    string? Email,
    bool IsEmailVerified,
    string? DisplayName,
    string? AvatarUrl);
