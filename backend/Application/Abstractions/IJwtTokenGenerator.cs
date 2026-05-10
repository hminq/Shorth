using System;
using Application.Dtos;
using Domain.Entities;

namespace Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
