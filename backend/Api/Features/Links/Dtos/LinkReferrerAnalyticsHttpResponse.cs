namespace Api.Features.Links.Dtos;

public sealed record LinkReferrerAnalyticsHttpResponse(
    string Source,
    string Label,
    int Clicks,
    decimal Percent);
