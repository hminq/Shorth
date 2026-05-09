using System;
using Domain.Entities.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class UserOtp
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public OtpPurpose Purpose { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? InvalidatedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime? LastSentAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserOtp() {}

    public static UserOtp Create(
        Guid userId,
        OtpPurpose purpose,
        string codeHash,
        int maxAttempts,
        DateTime createdAt,
        DateTime expiresAt
    )
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new ArgumentException("Otp code hash is required.", nameof(codeHash));
        }

        if (maxAttempts <= 0)
        {
            throw new ArgumentException("Max attempts must be greater than zero.", nameof(maxAttempts));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Expiration time must be greater than created time.", nameof(expiresAt));
        }

        return new UserOtp
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            CreatedAt = createdAt
        };
    }

    public void MarkUsed(DateTime usedAt)
    {
        EnsureCanBeModified(usedAt, nameof(usedAt));

        if (UsedAt.HasValue)
        {
            throw new InvalidOtpStateException("Otp has already been used.");
        }

        if (HasExceededAttempts())
        {
            throw new InvalidOtpStateException("Otp has exceeded the maximum number of attempts.");
        }

        UsedAt = usedAt;
    }

    public void Invalidate(DateTime invalidatedAt)
    {
        EnsureCanBeModified(invalidatedAt, nameof(invalidatedAt));

        if (InvalidatedAt.HasValue)
        {
            throw new InvalidOtpStateException("Otp has already been invalidated.");
        }

        if (UsedAt.HasValue)
        {
            throw new InvalidOtpStateException("Used otp cannot be invalidated.");
        }

        InvalidatedAt = invalidatedAt;
    }

    public void MarkSent(DateTime sentAt)
    {
        EnsureCanBeModified(sentAt, nameof(sentAt));

        if (HasExceededAttempts())
        {
            throw new InvalidOtpStateException("Cannot mark sent for otp that exceeded max attempts.");
        }

        LastSentAt = sentAt;
    }

    public void IncrementAttempt()
    {
        if (IsUsed())
        {
            throw new InvalidOtpStateException("Cannot increment attempts for a used otp.");
        }

        if (IsInvalidated())
        {
            throw new InvalidOtpStateException("Cannot increment attempts for an invalidated otp.");
        }

        if (HasExceededAttempts())
        {
            throw new InvalidOtpStateException("Otp has already exceeded the maximum number of attempts.");
        }

        AttemptCount++;
    }

    public bool IsExpired(DateTime at)
    {
        return at >= ExpiresAt;
    }

    public bool IsInvalidated()
    {
        return InvalidatedAt != null;
    }

    public bool HasExceededAttempts()
    {
        return AttemptCount >= MaxAttempts;
    }

    public bool IsUsed()
    {
        return UsedAt != null;
    }

    private void EnsureCanBeModified(DateTime at, string paramName)
    {
        if (at < CreatedAt)
        {
            throw new ArgumentException("Time cannot be earlier than created time.", paramName);
        }

        if (IsExpired(at))
        {
            throw new InvalidOtpStateException("Otp has expired.");
        }

        if (IsInvalidated())
        {
            throw new InvalidOtpStateException("Otp has been invalidated.");
        }
    }
}
