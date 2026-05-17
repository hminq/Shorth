using Application.Features.Links.Dtos;

namespace Application.Features.Links.Interfaces;

public interface ILinkCacheRepository
{
    Task<LinkCacheEntry?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task SetBySlugAsync(string slug, LinkCacheEntry entry, CancellationToken ct = default);
}
