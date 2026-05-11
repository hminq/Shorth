namespace Application.Features.Auth.Dtos;

public sealed record LocalRegisterRequest(
    string Email,
    string Password,
    string DisplayName
);
