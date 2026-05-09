using System;
using Application.Abstractions;
using Domain.Entities;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public class LinkRepository : ILinkRepository
{
    private readonly AppDbContext _dbContext;

    public LinkRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task AddAsync(Link link, CancellationToken ct = default)
    {
        try
        {
            await _dbContext.Links.AddAsync(link, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save link.", ex);
        }
    }

    public async Task<Link?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _dbContext.Links
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);
    }

    public async Task<IReadOnlyList<Link>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Links
            .AsNoTracking()
            .Where(x => x.OwnerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

}
