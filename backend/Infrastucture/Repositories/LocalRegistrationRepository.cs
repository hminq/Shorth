using Application.Features.Auth.Interfaces;
using Domain.Features.Auth.Entities;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class LocalRegistrationRepository : ILocalRegistrationRepository
{
    private readonly AppDbContext _dbContext;

    public LocalRegistrationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(User user, UserIdentity localIdentity, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.UserIdentities.AddAsync(localIdentity, ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to create local account.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
