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

    public Task<Link?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
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

    public Task IncrementClickCountAsync(Guid linkId, DateTime clickedAt, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
