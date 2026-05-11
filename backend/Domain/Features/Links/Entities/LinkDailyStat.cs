namespace Domain.Features.Links.Entities;

public class LinkDailyStat
{
    public Guid LinkId { get; private set; }
    public DateOnly Date { get; private set; }
    public int Clicks { get; private set; }
    public int UniqueVisitors { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private LinkDailyStat() {}

    public static LinkDailyStat Create(
        Guid linkId,
        DateOnly date,
        int clicks,
        int uniqueVisitors,
        DateTime createdAt
    )
    {
        if (linkId == Guid.Empty)
        {
            throw new ArgumentException("Link id is required.", nameof(linkId));
        }

        if (clicks < 0)
        {
            throw new ArgumentException("Invalid click number.", nameof(clicks));
        }

        if (uniqueVisitors < 0)
        {
            throw new ArgumentException("Invalid unique visitor number.", nameof(uniqueVisitors));
        }

        if (uniqueVisitors > clicks)
        {
            throw new ArgumentException("Visitors cannot be higher than clicks.", nameof(uniqueVisitors));
        }

        return new LinkDailyStat
        {
            LinkId = linkId,
            Date = date,
            Clicks = clicks,
            UniqueVisitors = uniqueVisitors,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateCounts(int clicks, int uniqueVisitors, DateTime updatedAt)
    {
        if (clicks < 0)
        {
            throw new ArgumentException("Invalid click number.", nameof(clicks));
        }

        if (uniqueVisitors < 0)
        {
            throw new ArgumentException("Invalid unique visitor number.", nameof(uniqueVisitors));
        }

        if (uniqueVisitors > clicks)
        {
            throw new ArgumentException("Visitors cannot be higher than clicks.", nameof(uniqueVisitors));
        }

        Clicks = clicks;
        UniqueVisitors = uniqueVisitors;
        UpdatedAt = updatedAt;
    }
}
