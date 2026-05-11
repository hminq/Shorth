using System;
using Domain.Features.Links.Entities;

namespace Application.Features.Links.Interfaces;

public interface ILinkClickEventRepository
{
    Task<IReadOnlyList<LinkClickEvent>> GetByLinkIdAsync(Guid linkId, CancellationToken ct = default);
    Task SaveClickEventAsync(LinkClickEvent clickEvent, CancellationToken ct = default);
}
