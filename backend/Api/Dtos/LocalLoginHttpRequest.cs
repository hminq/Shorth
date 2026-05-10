using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public sealed class LocalLoginHttpRequest
{
    private const string PasswordRegex = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*(?:\d|[^A-Za-z0-9]))\S+$";

    [Required]
    [EmailAddress]
    public string Email { get; init; } = default!;

    [Required]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 72 characters.")]
    [RegularExpression(
        PasswordRegex,
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number or special character, with no whitespace.")]
    public string Password { get; init; } = default!;
}
