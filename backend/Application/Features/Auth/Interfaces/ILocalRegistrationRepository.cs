using Domain.Features.Auth.Entities;

namespace Application.Features.Auth.Interfaces;

public interface ILocalRegistrationRepository
{
    Task CreateAsync(User user, UserIdentity localIdentity, CancellationToken ct = default);
}
