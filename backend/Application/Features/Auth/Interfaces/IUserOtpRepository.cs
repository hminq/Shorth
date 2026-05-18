using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Application.Features.Auth.Messages;

namespace Application.Features.Auth.Interfaces;

public interface IUserOtpRepository
{
    Task<UserOtp?> GetLatestByUserIdAndPurposeAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    Task RefreshAsync(UserOtp? existingOtp, UserOtp newOtp, CancellationToken ct = default);

    Task RefreshWithEmailJobAsync(
        UserOtp? existingOtp,
        UserOtp newOtp,
        EmailJobMessage emailJob,
        CancellationToken ct = default);

    Task CompleteEmailVerificationAsync(User user, UserOtp otp, CancellationToken ct = default);

    Task CompletePasswordResetVerificationAsync(UserOtp otp, CancellationToken ct = default);

    Task IncrementAttemptAsync(UserOtp otp, CancellationToken ct = default);
}
