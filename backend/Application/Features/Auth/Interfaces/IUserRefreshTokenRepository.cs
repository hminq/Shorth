using Domain.Features.Auth.Entities;

namespace Application.Features.Auth.Interfaces;

public interface IUserRefreshTokenRepository
{
    Task<UserRefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(UserRefreshToken refreshToken, CancellationToken ct = default);

    Task RotateAsync(
        UserRefreshToken currentRefreshToken,
        UserRefreshToken replacementRefreshToken,
        CancellationToken ct = default);

    Task RevokeAsync(UserRefreshToken refreshToken, CancellationToken ct = default);

    Task RevokeActiveByUserIdAsync(Guid userId, DateTime revokedAt, CancellationToken ct = default);
}
