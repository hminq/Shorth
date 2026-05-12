using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Dtos;

public sealed class VerifyEmailOtpHttpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = default!;

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Otp code must be exactly 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Otp code must be a 6-digit number.")]
    public string OtpCode { get; init; } = default!;
}
