using Application.Features.Auth.Interfaces;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Exceptions;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastucture.Repositories;

public sealed class ExternalIdentityRepository : IExternalIdentityRepository
{
    private readonly AppDbContext _dbContext;

    public ExternalIdentityRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(
        User user,
        UserIdentity googleIdentity,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.UserIdentities.AddAsync(googleIdentity, ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);

            if (IsUniqueViolation(ex))
            {
                throw new EmailAlreadyExistedException("This email already has an account.");
            }

            throw new InvalidOperationException("Failed to create Google account.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task LinkAsync(
        User user,
        UserIdentity googleIdentity,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.Users.Update(user);
            await _dbContext.UserIdentities.AddAsync(googleIdentity, ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);

            if (IsUniqueViolation(ex))
            {
                throw new EmailAlreadyExistedException("This Google account is already linked.");
            }

            throw new InvalidOperationException("Failed to link Google account.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
