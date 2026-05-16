using Application.Features.Auth.Interfaces;
using Domain.Features.Auth.Entities;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public async Task<User?> GetByEmailNormalizedAsync(string emailNormalized, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(x => x.EmailNormalized == emailNormalized, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save user.", ex);
        }
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        try
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to update user.", ex);
        }
    }

    public async Task UpdateWithNewIdentityAsync(User user, UserIdentity identity, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.Users.Update(user);
            await _dbContext.UserIdentities.AddAsync(identity, ct);
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to update user with identity.", ex);
        }
    }
}
