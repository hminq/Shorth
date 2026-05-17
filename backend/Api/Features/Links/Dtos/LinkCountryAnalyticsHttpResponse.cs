namespace Api.Features.Links.Dtos;

public sealed record LinkCountryAnalyticsHttpResponse(
    string CountryCode,
    int Clicks,
    decimal Percent);
