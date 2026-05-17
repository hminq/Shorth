using Application.Features.Auth.Messages;
using Domain.Features.Auth.Entities;

namespace Application.Features.Auth.Interfaces;

public interface ILocalRegistrationRepository
{
    Task CreateAsync(
        User user,
        UserIdentity localIdentity,
        UserOtp emailVerificationOtp,
        EmailJobMessage emailJob,
        CancellationToken ct = default);

    Task RefreshPendingVerificationAsync(
        User user,
        UserIdentity localIdentity,
        UserOtp emailVerificationOtp,
        EmailJobMessage emailJob,
        CancellationToken ct = default);
}
