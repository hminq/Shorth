using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Dtos;

public sealed class CompletePasswordResetHttpRequest
{
    private const string PasswordRegex = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$";

    [Required]
    public string ResetToken { get; init; } = default!;

    [Required]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 72 characters.")]
    [RegularExpression(
        PasswordRegex,
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character, with no whitespace.")]
    public string NewPassword { get; init; } = default!;
}
