namespace Application.Features.Links.Dtos;

public sealed record LinkCountryAnalyticsItem(
    string CountryCode,
    int Clicks,
    decimal Percent);
