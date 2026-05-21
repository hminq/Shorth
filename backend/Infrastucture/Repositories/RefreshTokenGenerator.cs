using System.Security.Cryptography;
using Application.Features.Auth.Interfaces;

namespace Infrastucture.Repositories;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenByteLength = 64;

    public string GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
