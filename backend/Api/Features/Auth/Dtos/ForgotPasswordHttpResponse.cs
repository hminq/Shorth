namespace Api.Features.Auth.Dtos;

public sealed record ForgotPasswordHttpResponse(
    string Email,
    string Message);
