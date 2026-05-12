using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;

namespace Application.Features.Auth.Interfaces;

public interface IUserOtpRepository
{
    Task<UserOtp?> GetLatestByUserIdAndPurposeAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    Task RefreshAsync(UserOtp? existingOtp, UserOtp newOtp, CancellationToken ct = default);

    Task CompleteEmailVerificationAsync(User user, UserOtp otp, CancellationToken ct = default);

    Task IncrementAttemptAsync(UserOtp otp, CancellationToken ct = default);
}
