using System.Text.Json;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Exceptions;
using Domain.Features.Outbox.Entities;
using Domain.Features.Outbox.Enums;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastucture.Repositories;

public sealed class LocalRegistrationRepository : ILocalRegistrationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;

    public LocalRegistrationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(
        User user,
        UserIdentity localIdentity,
        UserOtp emailVerificationOtp,
        EmailJobMessage emailJob,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.UserIdentities.AddAsync(localIdentity, ct);
            await _dbContext.UserOtps.AddAsync(emailVerificationOtp, ct);
            await _dbContext.OutboxMessages.AddAsync(CreateEmailJobOutboxMessage(emailJob, user.CreatedAt), ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);

            if (ex.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new EmailAlreadyExistedException("This email already has an account.");
            }

            throw new InvalidOperationException("Failed to create account.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RefreshPendingVerificationAsync(
        User user,
        UserIdentity localIdentity,
        UserOtp emailVerificationOtp,
        EmailJobMessage emailJob,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.Users.Update(user);
            _dbContext.UserIdentities.Update(localIdentity);
            await _dbContext.UserOtps.AddAsync(emailVerificationOtp, ct);
            await _dbContext.OutboxMessages.AddAsync(CreateEmailJobOutboxMessage(emailJob, emailVerificationOtp.CreatedAt), ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to refresh pending local account verification.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static OutboxMessage CreateEmailJobOutboxMessage(EmailJobMessage emailJob, DateTime createdAt)
    {
        var payload = JsonSerializer.Serialize(emailJob, JsonOptions);
        return OutboxMessage.Create(OutboxMessageType.EmailJob, payload, createdAt);
    }
}
