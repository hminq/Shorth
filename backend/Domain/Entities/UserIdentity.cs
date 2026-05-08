using Domain.Entities.Enums;

namespace Domain.Entities;

public class UserIdentity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public IdentityProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; } = null!;
    public string? ProviderEmail { get; private set; }
    public string? PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserIdentity() { }

    public static UserIdentity CreateLocal(
        Guid userId,
        string email,
        string emailNormalized,
        string passwordHash,
        DateTime createdAt
    )
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(emailNormalized))
        {
            throw new ArgumentException("Normalized email is required.", nameof(emailNormalized));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = IdentityProvider.Local,
            ProviderUserId = emailNormalized, // use user registered email for provider id
            ProviderEmail = email,
            PasswordHash = passwordHash,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public static UserIdentity CreateExternal(
        Guid userId,
        IdentityProvider provider,
        string providerUserId,
        string? providerEmail,
        DateTime createdAt
    )
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (provider == IdentityProvider.Local)
        {
            throw new ArgumentException("Wrong provider type.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            throw new ArgumentException("Provider user id is required.", nameof(providerUserId));
        }

        return new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId, 
            ProviderEmail = string.IsNullOrWhiteSpace(providerEmail) ? null : providerEmail,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdatePasswordHash(string passwordHash, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        UpdatedAt = updatedAt;
    }

    public void UpdateProviderEmail(string? email, DateTime updatedAt)
    {
        ProviderEmail = string.IsNullOrWhiteSpace(email) ? null : email;
        UpdatedAt = updatedAt;
    }
}
