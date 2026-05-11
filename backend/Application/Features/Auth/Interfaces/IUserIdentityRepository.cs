using System;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;

namespace Application.Features.Auth.Interfaces;

public interface IUserIdentityRepository
{
    Task<UserIdentity?> GetLocalByEmailNormalizedAsync(string emailNormalized, CancellationToken ct = default);
    Task<UserIdentity?> GetByProviderAndProviderUserIdAsync(
        IdentityProvider provider,
        string providerUserId,
        CancellationToken ct = default);
    Task<IReadOnlyList<UserIdentity>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserIdentity identity, CancellationToken ct = default);
}
