using Application.Features.Auth.Dtos;
using Application.Features.Auth.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class GoogleAuthProvider : IGoogleAuthProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private static readonly string[] Scopes =
    [
        Oauth2Service.Scope.Openid,
        Oauth2Service.Scope.UserinfoEmail,
        Oauth2Service.Scope.UserinfoProfile
    ];

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GoogleAuthProvider(GoogleAuthOptions options)
    {
        _clientId = options.ClientId;
        _clientSecret = options.ClientSecret;
        _redirectUri = options.RedirectUri;
    }

    public string BuildAuthorizationUrl(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State is required.", nameof(state));
        }

        var scope = string.Join(' ', Scopes);

        return $"{AuthorizationEndpoint}" +
               $"?client_id={Uri.EscapeDataString(_clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(scope)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&access_type=offline" +
               $"&include_granted_scopes=true" +
               $"&prompt=select_account";
    }

    public async Task<GoogleUserInfo> GetUserInfoAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Authorization code is required.", nameof(request));
        }

        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = Scopes
            });

        var tokenResponse = await flow.ExchangeCodeForTokenAsync(
            userId: string.Empty,
            code: request.Code,
            redirectUri: _redirectUri,
            taskCancellationToken: ct);

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Google access token is missing.");
        }

        var credential = GoogleCredential.FromAccessToken(tokenResponse.AccessToken);
        var oauthService = new Oauth2Service(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Shorth"
            });

        var userInfo = await oauthService.Userinfo.Get().ExecuteAsync(ct);
        if (userInfo is null || string.IsNullOrWhiteSpace(userInfo.Id))
        {
            throw new InvalidOperationException("Google user info is missing.");
        }

        return new GoogleUserInfo(
            userInfo.Id,
            string.IsNullOrWhiteSpace(userInfo.Email) ? null : userInfo.Email,
            userInfo.VerifiedEmail ?? false,
            string.IsNullOrWhiteSpace(userInfo.Name) ? null : userInfo.Name,
            string.IsNullOrWhiteSpace(userInfo.Picture) ? null : userInfo.Picture);
    }
}
