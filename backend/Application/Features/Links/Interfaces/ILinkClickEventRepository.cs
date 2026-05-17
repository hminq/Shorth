using System;
using Application.Features.Links.Dtos;
using Domain.Features.Links.Entities;

namespace Application.Features.Links.Interfaces;

public interface ILinkClickEventRepository
{
    Task<IReadOnlyList<LinkClickEvent>> GetByLinkIdAsync(Guid linkId, CancellationToken ct = default);
    Task SaveClickEventAsync(LinkClickEvent clickEvent, CancellationToken ct = default);
    Task SaveClickAndUpdateAnalyticsAsync(LinkClickEvent clickEvent, CancellationToken ct = default);
    Task<IReadOnlyList<LinkDailyAnalyticsItem>> GetDailyAnalyticsAsync(
        Guid linkId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
    Task<IReadOnlyList<LinkCountryAnalyticsItem>> GetTopCountriesAsync(
        Guid linkId,
        DateTime from,
        DateTime toExclusive,
        int take,
        CancellationToken ct = default);
}
