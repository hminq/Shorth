using System;

namespace Domain.Entities;

public class Link
{
    public Guid Id { get; private set; }
    public Guid? OwnerId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string DestinationUrl { get; private set; } = null!;
    public long ClickCount { get; private set; }
    public DateTime? LastClickedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsDisabled { get; private set; }

    private Link() {}

    public static Link Create(
        Guid? ownerId,
        string slug,
        string destinationUrl,
        DateTime createdAt,
        DateTime? expiresAt
    )
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length != 6)
        {
            throw new ArgumentException("Slug must be a 6 characters string.", nameof(slug));
        }

        if (string.IsNullOrWhiteSpace(destinationUrl))
        {
            throw new ArgumentException("Destination url is required.", nameof(destinationUrl));
        }

        if (expiresAt.HasValue && expiresAt.Value < createdAt)
        {
            throw new ArgumentException("Expire time must after creation time.", nameof(expiresAt));
        }

        return new Link
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Slug = slug,
            DestinationUrl = destinationUrl,
            ClickCount = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            ExpiresAt = expiresAt,
            IsDisabled = false
        };
    }

    public void Disable(DateTime disabledAt)
    {
        IsDisabled = true;
        UpdatedAt = disabledAt;
    }

    public void UpdateDestination(string newDestination, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(newDestination))
        {
            throw new ArgumentException("New destination url is required.", nameof(newDestination));
        }

        DestinationUrl = newDestination;
        UpdatedAt = updatedAt;
    }

    public void RegisterClick(DateTime clickedAt)
    {
        ClickCount++;
        LastClickedAt = clickedAt;
        UpdatedAt = clickedAt;
    }
}
