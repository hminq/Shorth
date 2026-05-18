using Application.Features.Auth.Dtos;

namespace Application.Features.Auth.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task StoreAsync(
        string token,
        PasswordResetTokenState state,
        CancellationToken ct = default);

    Task<PasswordResetTokenState?> TakeAsync(
        string token,
        CancellationToken ct = default);
}
