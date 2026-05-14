using Domain.Features.Auth.Entities;

namespace Application.Features.Auth.Interfaces;

public interface IExternalIdentityRepository
{
    Task CreateAsync(
        User user,
        UserIdentity googleIdentity,
        CancellationToken ct = default);

    Task LinkAsync(
        User user,
        UserIdentity googleIdentity,
        CancellationToken ct = default);
}
