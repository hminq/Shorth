using Application.Abstractions;
using Domain.Entities;
using Domain.Entities.Enums;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class UserIdentityRepository : IUserIdentityRepository
{
    private readonly AppDbContext _dbContext;

    public UserIdentityRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserIdentity?> GetLocalByEmailNormalizedAsync(string emailNormalized, CancellationToken ct = default)
    {
        return await _dbContext.UserIdentities
            .FirstOrDefaultAsync(
                x => x.Provider == IdentityProvider.Local && x.ProviderUserId == emailNormalized,
                ct);
    }

    public async Task<UserIdentity?> GetByProviderAndProviderUserIdAsync(
        IdentityProvider provider,
        string providerUserId,
        CancellationToken ct = default)
    {
        return await _dbContext.UserIdentities
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderUserId == providerUserId,
                ct);
    }

    public async Task<IReadOnlyList<UserIdentity>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.UserIdentities
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(UserIdentity identity, CancellationToken ct = default)
    {
        try
        {
            await _dbContext.UserIdentities.AddAsync(identity, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save user identity.", ex);
        }
    }
}
