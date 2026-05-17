namespace Application.Features.Links.Dtos;

public sealed record LinkDailyAnalyticsItem(
    DateOnly Date,
    int Clicks,
    int UniqueVisitors);
