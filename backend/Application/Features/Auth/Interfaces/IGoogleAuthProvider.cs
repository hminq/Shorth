using Application.Features.Auth.Dtos;

namespace Application.Features.Auth.Interfaces;

public interface IGoogleAuthProvider
{
    string BuildAuthorizationUrl(string state);

    Task<GoogleUserInfo> GetUserInfoAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default);
}
