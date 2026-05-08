using System;
using Domain.Entities.Enums;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string? Email { get; private set; }
    public string? EmailNormalized { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public UserStatus Status { get; private set; } 
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() {}
    
    public static User CreateLocal(
        string email,
        string emailNormalized,
        string? displayName,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(emailNormalized))
        {
            throw new ArgumentException("Normalized email is required.", nameof(emailNormalized));
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            EmailNormalized = emailNormalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            Status = UserStatus.PendingVerification, // must verify Email
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public static User CreateOAuth(
        IdentityProvider provider,
        string? email,
        string? emailNormalized,
        string? displayName,
        string? avatarUrl,       
        DateTime createdAt
    )
    {
        if (provider == IdentityProvider.Local)
        {
            throw new ArgumentException("This method cannot be used for local identity.", nameof(provider));
        }

        if (!string.IsNullOrWhiteSpace(emailNormalized) && string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Normalized email cannot be provided without email.", nameof(emailNormalized));
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            EmailNormalized = string.IsNullOrWhiteSpace(emailNormalized) ? null : emailNormalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Status = UserStatus.Active // No need to verify Email
        };
    }

    public void VerifyEmail(DateTime verifiedAt)
    {
        if (EmailVerifiedAt.HasValue) return;

        EmailVerifiedAt = verifiedAt;
        Status = UserStatus.Active;
        UpdatedAt = verifiedAt;
    }

    public void MarkLastLogin(DateTime lastLoginAt)
    {
        LastLoginAt = lastLoginAt;
        UpdatedAt = lastLoginAt;
    }

    public void Disable(DateTime disabledAt)
    {
        Status = UserStatus.Disabled;
        UpdatedAt = disabledAt;
    }

    public void UpdateProfile(string? displayName, string? avatarUrl, DateTime updatedAt)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
        UpdatedAt = updatedAt;
    }
}
