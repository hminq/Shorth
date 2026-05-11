using Domain.Features.Links.Entities;

namespace Application.Features.Links.Interfaces;

public interface ILinkRepository
{
    Task AddAsync(Link link, CancellationToken ct = default);
    Task<Link?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Link?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Link>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task IncrementClickCountAsync(Guid linkId, DateTime clickedAt, CancellationToken ct = default);
}
