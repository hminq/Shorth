using System;
using Application.Features.Links.Interfaces;
using Domain.Features.Links.Entities;
using Domain.Features.Links.Exceptions;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException &&
                                           postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateSlugException("Generated slug already exists.");
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save link.", ex);
        }
    }

    public async Task<Link?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Links
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Link?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _dbContext.Links
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);
    }

    public async Task<IReadOnlyList<Link>> GetByOwnerIdAsync(
        Guid ownerId,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        return await _dbContext.Links
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task IncrementClickCountAsync(Guid linkId, DateTime clickedAt, CancellationToken ct = default)
    {
        await _dbContext.Links
            .Where(x => x.Id == linkId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ClickCount, x => x.ClickCount + 1)
                .SetProperty(x => x.LastClickedAt, clickedAt)
                .SetProperty(x => x.UpdatedAt, clickedAt), ct);
    }
}
