using Application.Features.Auth.Interfaces;
using Domain.Features.Auth.Entities;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class UserRefreshTokenRepository : IUserRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public UserRefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserRefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return await _dbContext.UserRefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
    }

    public async Task AddAsync(UserRefreshToken refreshToken, CancellationToken ct = default)
    {
        try
        {
            await _dbContext.UserRefreshTokens.AddAsync(refreshToken, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save refresh token.", ex);
        }
    }

    public async Task RotateAsync(
        UserRefreshToken currentRefreshToken,
        UserRefreshToken replacementRefreshToken,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            await _dbContext.UserRefreshTokens.AddAsync(replacementRefreshToken, ct);
            await _dbContext.SaveChangesAsync(ct);

            _dbContext.UserRefreshTokens.Update(currentRefreshToken);
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to rotate refresh token.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RevokeAsync(UserRefreshToken refreshToken, CancellationToken ct = default)
    {
        try
        {
            _dbContext.UserRefreshTokens.Update(refreshToken);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to revoke refresh token.", ex);
        }
    }

    public async Task RevokeActiveByUserIdAsync(Guid userId, DateTime revokedAt, CancellationToken ct = default)
    {
        var activeTokens = await _dbContext.UserRefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > revokedAt)
            .ToListAsync(ct);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(revokedAt);
        }

        if (activeTokens.Count == 0)
        {
            return;
        }

        try
        {
            _dbContext.UserRefreshTokens.UpdateRange(activeTokens);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to revoke active refresh tokens.", ex);
        }
    }
}
