namespace Api.Features.Links.Dtos;

public sealed record LinkAnalyticsHttpResponse(
    Guid LinkId,
    long TotalClicks,
    DateTime? LastClickedAt,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<LinkDailyAnalyticsHttpResponse> Daily,
    IReadOnlyList<LinkCountryAnalyticsHttpResponse> TopCountries);
