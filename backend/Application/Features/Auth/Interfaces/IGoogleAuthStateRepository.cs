using Application.Features.Auth.Dtos;

namespace Application.Features.Auth.Interfaces;

public interface IGoogleAuthStateRepository
{
    Task StoreAsync(
        string state,
        GoogleAuthState authState,
        CancellationToken ct = default);

    Task<GoogleAuthState?> TakeAsync(
        string state,
        CancellationToken ct = default);
}
