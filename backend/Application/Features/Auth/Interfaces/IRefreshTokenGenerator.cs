namespace Application.Features.Auth.Interfaces;

public interface IRefreshTokenGenerator
{
    string GenerateToken();
}
