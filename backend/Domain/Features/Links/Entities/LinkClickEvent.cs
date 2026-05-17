using System;

namespace Domain.Features.Links.Entities;

public class LinkClickEvent
{
    public Guid Id { get; private set; }
    public Guid LinkId { get; private set; }
    public DateTime ClickedAt { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Referrer { get; private set; }
    public string? IpHash { get; private set; }
    public string? CountryCode { get; private set; }
    public string? DeviceType { get; private set; }
    public string? BrowserFamily { get; private set; }
    public string? OsFamily { get; private set; }

    private LinkClickEvent() {}

    public static LinkClickEvent Create(
        Guid id,
        Guid linkId,
        DateTime clickedAt,
        string? userAgent,
        string? referrer,
        string? ipHash,
        string? countryCode,
        string? deviceType,
        string? browserFamily,
        string? osFamily
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Click event id is required.", nameof(id));
        }

        if (linkId == Guid.Empty)
        {
            throw new ArgumentException("Link id is required.", nameof(linkId));
        }

        return new LinkClickEvent
        {
            Id = id,
            LinkId = linkId,
            ClickedAt = clickedAt,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            Referrer = string.IsNullOrWhiteSpace(referrer) ? null : referrer,
            IpHash = string.IsNullOrWhiteSpace(ipHash) ? null : ipHash,
            CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode,
            DeviceType = string.IsNullOrWhiteSpace(deviceType) ? null : deviceType,
            BrowserFamily = string.IsNullOrWhiteSpace(browserFamily) ? null : browserFamily,
            OsFamily = string.IsNullOrWhiteSpace(osFamily) ? null : osFamily
        };
    }
}
