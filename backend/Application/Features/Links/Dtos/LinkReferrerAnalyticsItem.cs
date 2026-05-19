namespace Application.Features.Links.Dtos;

public sealed record LinkReferrerAnalyticsItem(
    string Source,
    string Label,
    int Clicks,
    decimal Percent);
