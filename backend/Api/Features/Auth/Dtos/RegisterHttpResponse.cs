namespace Api.Features.Auth.Dtos;

public sealed record RegisterHttpResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    bool RequiresEmailVerification
);
