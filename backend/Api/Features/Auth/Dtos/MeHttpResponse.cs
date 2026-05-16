namespace Api.Features.Auth.Dtos;

public sealed record MeHttpResponse(
    Guid UserId,
    string Email,
    string DisplayName);
