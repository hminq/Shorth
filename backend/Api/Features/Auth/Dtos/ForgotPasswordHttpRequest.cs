using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Dtos;

public sealed class ForgotPasswordHttpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = default!;
}
