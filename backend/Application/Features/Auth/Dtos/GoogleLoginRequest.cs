namespace Application.Features.Auth.Dtos;

public sealed record GoogleLoginRequest(
    string Code,
    string? State);
