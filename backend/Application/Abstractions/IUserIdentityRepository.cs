using System;
using Domain.Entities;
using Domain.Entities.Enums;

namespace Application.Abstractions;

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
