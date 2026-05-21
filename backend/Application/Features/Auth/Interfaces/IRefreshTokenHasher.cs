namespace Application.Features.Auth.Interfaces;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}
