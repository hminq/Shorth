namespace Domain.Features.Auth.Entities;

public class UserRefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserRefreshToken() {}

    public static UserRefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Refresh token hash is required.", nameof(tokenHash));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Expiration time must be greater than created time.", nameof(expiresAt));
        }

        return new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Revoke(DateTime revokedAt)
    {
        EnsureNotRevoked();

        if (revokedAt < CreatedAt)
        {
            throw new ArgumentException("Revoked time cannot be earlier than created time.", nameof(revokedAt));
        }

        RevokedAt = revokedAt;
        UpdatedAt = revokedAt;
    }

    public void Replace(Guid replacementTokenId, DateTime replacedAt)
    {
        if (replacementTokenId == Guid.Empty)
        {
            throw new ArgumentException("Replacement token id is required.", nameof(replacementTokenId));
        }

        Revoke(replacedAt);
        ReplacedByTokenId = replacementTokenId;
    }

    public bool IsExpired(DateTime at)
    {
        return at >= ExpiresAt;
    }

    public bool IsRevoked()
    {
        return RevokedAt.HasValue;
    }

    public bool IsActive(DateTime at)
    {
        return !IsRevoked() && !IsExpired(at);
    }

    private void EnsureNotRevoked()
    {
        if (IsRevoked())
        {
            throw new InvalidOperationException("Refresh token has already been revoked.");
        }
    }
}
