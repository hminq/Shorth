using Domain.Entities;

namespace Application.Abstractions;

public interface ILinkRepository
{
    Task AddAsync(Link link, CancellationToken ct = default);
    Task<Link?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Link>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
