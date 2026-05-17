using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Dtos;

public sealed class LocalLoginHttpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = default!;

    [Required]
    [StringLength(72, MinimumLength = 1, ErrorMessage = "Password is required.")]
    public string Password { get; init; } = default!;
}
