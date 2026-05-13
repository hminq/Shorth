using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Features.Auth.Interfaces;
using Domain.Features.Auth.Entities;
using Infrastucture.Configurations;
using Microsoft.IdentityModel.Tokens;

namespace Infrastucture.Repositories;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private const string UserIdClaim = "user_id";
    private const string EmailClaim = "email";
    private const string DisplayNameClaim = "display_name";

    private readonly byte[] _signingKeyBytes;
    private readonly TimeSpan _accessTokenTtl;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenGenerator(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new ArgumentException("JWT signing key is required.", nameof(options));
        }

        var accessTokenTtl = TimeSpan.FromMinutes(options.AccessTokenTtlMinutes);
        if (accessTokenTtl <= TimeSpan.Zero)
        {
            throw new ArgumentException("JWT access token ttl must be positive.", nameof(options));
        }

        _signingKeyBytes = Encoding.UTF8.GetBytes(options.SigningKey);
        _accessTokenTtl = accessTokenTtl;
    }

    public string GenerateToken(User user)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(UserIdClaim, user.Id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(EmailClaim, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new Claim(DisplayNameClaim, user.DisplayName));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.Add(_accessTokenTtl),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_signingKeyBytes),
                SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(descriptor);
        return _tokenHandler.WriteToken(token);
    }
}
