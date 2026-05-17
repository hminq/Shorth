namespace Api.Features.Links.Dtos;

public sealed record LinkDailyAnalyticsHttpResponse(
    DateOnly Date,
    int Clicks,
    int UniqueVisitors);
