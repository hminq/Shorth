using Application.Features.Links.Interfaces;
using Application.Features.Links.Dtos;
using Domain.Features.Links.Entities;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class LinkClickEventRepository : ILinkClickEventRepository
{
    private readonly AppDbContext _dbContext;

    public LinkClickEventRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LinkClickEvent>> GetByLinkIdAsync(Guid linkId, CancellationToken ct = default)
    {
        return await _dbContext.LinkClickEvents
            .AsNoTracking()
            .Where(x => x.LinkId == linkId)
            .OrderByDescending(x => x.ClickedAt)
            .ToListAsync(ct);
    }

    public async Task SaveClickEventAsync(LinkClickEvent clickEvent, CancellationToken ct = default)
    {
        await _dbContext.LinkClickEvents.AddAsync(clickEvent, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task SaveClickAndUpdateAnalyticsAsync(LinkClickEvent clickEvent, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        var inserted = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO link_click_events
                (id, link_id, clicked_at, user_agent, referrer, ip_hash, country_code, device_type, browser_family, os_family)
            VALUES
                ({clickEvent.Id}, {clickEvent.LinkId}, {clickEvent.ClickedAt}, {clickEvent.UserAgent}, {clickEvent.Referrer}, {clickEvent.IpHash}, {clickEvent.CountryCode}, {clickEvent.DeviceType}, {clickEvent.BrowserFamily}, {clickEvent.OsFamily})
            ON CONFLICT (id) DO NOTHING
            """, ct);

        if (inserted == 0)
        {
            await transaction.CommitAsync(ct);
            return;
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE links
            SET click_count = click_count + 1,
                last_clicked_at = {clickEvent.ClickedAt},
                updated_at = {clickEvent.ClickedAt}
            WHERE id = {clickEvent.LinkId}
            """, ct);

        var date = DateOnly.FromDateTime(clickEvent.ClickedAt);
        var dayStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO link_daily_stats (link_id, date, clicks, unique_visitors, created_at, updated_at)
            SELECT
                {clickEvent.LinkId},
                {date},
                COUNT(*)::integer,
                COUNT(DISTINCT ip_hash)::integer,
                {clickEvent.ClickedAt},
                {clickEvent.ClickedAt}
            FROM link_click_events
            WHERE link_id = {clickEvent.LinkId}
              AND clicked_at >= {dayStart}
              AND clicked_at < {dayEnd}
            ON CONFLICT (link_id, date) DO UPDATE
            SET clicks = EXCLUDED.clicks,
                unique_visitors = EXCLUDED.unique_visitors,
                updated_at = EXCLUDED.updated_at
            """, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<LinkDailyAnalyticsItem>> GetDailyAnalyticsAsync(
        Guid linkId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        return await _dbContext.LinkDailyStats
            .AsNoTracking()
            .Where(x => x.LinkId == linkId && x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .Select(x => new LinkDailyAnalyticsItem(x.Date, x.Clicks, x.UniqueVisitors))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LinkCountryAnalyticsItem>> GetTopCountriesAsync(
        Guid linkId,
        DateTime from,
        DateTime toExclusive,
        int take,
        CancellationToken ct = default)
    {
        var grouped = await _dbContext.LinkClickEvents
            .AsNoTracking()
            .Where(x => x.LinkId == linkId
                        && x.ClickedAt >= from
                        && x.ClickedAt < toExclusive
                        && x.CountryCode != null)
            .GroupBy(x => x.CountryCode!)
            .Select(group => new
            {
                CountryCode = group.Key,
                Clicks = group.Count()
            })
            .OrderByDescending(x => x.Clicks)
            .ThenBy(x => x.CountryCode)
            .ToListAsync(ct);

        var totalKnownCountryClicks = grouped.Sum(x => x.Clicks);
        if (totalKnownCountryClicks == 0)
        {
            return [];
        }

        return grouped
            .Take(take)
            .Select(x => new LinkCountryAnalyticsItem(
                x.CountryCode,
                x.Clicks,
                decimal.Round(x.Clicks * 100m / totalKnownCountryClicks, 2)))
            .ToList();
    }
}
