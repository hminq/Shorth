using Domain.Features.Auth.Entities;

namespace Application.Features.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
