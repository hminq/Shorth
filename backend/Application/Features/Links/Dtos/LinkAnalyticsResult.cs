namespace Application.Features.Links.Dtos;

public sealed record LinkAnalyticsResult(
    Guid LinkId,
    long TotalClicks,
    DateTime? LastClickedAt,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<LinkDailyAnalyticsItem> Daily,
    IReadOnlyList<LinkCountryAnalyticsItem> TopCountries);
