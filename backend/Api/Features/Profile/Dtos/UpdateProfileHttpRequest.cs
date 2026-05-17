using System.ComponentModel.DataAnnotations;

namespace Api.Features.Profile.Dtos;

public sealed class UpdateProfileHttpRequest : IValidatableObject
{
    private const string PasswordRegex = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$";
    private string? _displayName;
    private string? _avatarUrl;

    [StringLength(100, MinimumLength = 1)]
    public string? DisplayName
    {
        get => _displayName;
        init => _displayName = value?.Trim();
    }

    [StringLength(500)]
    public string? AvatarUrl
    {
        get => _avatarUrl;
        init => _avatarUrl = value?.Trim();
    }

    [StringLength(72)]
    public string? CurrentPassword { get; init; }

    [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 72 characters.")]
    [RegularExpression(
        PasswordRegex,
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character, with no whitespace.")]
    public string? NewPassword { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AvatarUrl is null)
        {
            yield break;
        }

        if (!Uri.TryCreate(AvatarUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            yield return new ValidationResult(
                "Avatar url must be an absolute http or https URL.",
                [nameof(AvatarUrl)]);
        }
    }
}
