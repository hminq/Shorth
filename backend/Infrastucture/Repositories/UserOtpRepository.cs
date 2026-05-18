using System.Text.Json;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Domain.Features.Outbox.Entities;
using Domain.Features.Outbox.Enums;
using Infrastucture.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Repositories;

public sealed class UserOtpRepository : IUserOtpRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;

    public UserOtpRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserOtp?> GetLatestByUserIdAndPurposeAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        return await _dbContext.UserOtps
            .Where(x => x.UserId == userId && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task RefreshAsync(UserOtp? existingOtp, UserOtp newOtp, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            if (existingOtp is not null)
            {
                _dbContext.UserOtps.Update(existingOtp);
            }

            await _dbContext.UserOtps.AddAsync(newOtp, ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to refresh email verification otp.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RefreshWithEmailJobAsync(
        UserOtp? existingOtp,
        UserOtp newOtp,
        EmailJobMessage emailJob,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            if (existingOtp is not null)
            {
                _dbContext.UserOtps.Update(existingOtp);
            }

            await _dbContext.UserOtps.AddAsync(newOtp, ct);
            await _dbContext.OutboxMessages.AddAsync(CreateEmailJobOutboxMessage(emailJob, newOtp.CreatedAt), ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to refresh password reset otp.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CompleteEmailVerificationAsync(User user, UserOtp otp, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.Users.Update(user);
            _dbContext.UserOtps.Update(otp);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Failed to complete email verification.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CompletePasswordResetVerificationAsync(UserOtp otp, CancellationToken ct = default)
    {
        try
        {
            _dbContext.UserOtps.Update(otp);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to complete password reset verification.", ex);
        }
    }

    public async Task IncrementAttemptAsync(UserOtp otp, CancellationToken ct = default)
    {
        try
        {
            _dbContext.UserOtps.Update(otp);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to record otp attempt.", ex);
        }
    }

    private static OutboxMessage CreateEmailJobOutboxMessage(EmailJobMessage emailJob, DateTime createdAt)
    {
        var payload = JsonSerializer.Serialize(emailJob, JsonOptions);
        return OutboxMessage.Create(OutboxMessageType.EmailJob, payload, createdAt);
    }
}
