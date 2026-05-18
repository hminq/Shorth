using Application.Features.Auth.Interfaces;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class AuthLinkBuilder : IAuthLinkBuilder
{
    private readonly string _webBaseUrl;

    public AuthLinkBuilder(WebClientOptions options)
    {
        _webBaseUrl = options.BaseUrl.TrimEnd('/');
    }

    public string BuildPasswordResetUrl(string email)
    {
        return $"{_webBaseUrl}/password-reset?email={Uri.EscapeDataString(email)}";
    }
}
