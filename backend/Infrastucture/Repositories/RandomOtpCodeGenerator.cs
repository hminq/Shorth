using System.Security.Cryptography;
using Application.Features.Auth.Interfaces;

namespace Infrastucture.Repositories;

public sealed class RandomOtpCodeGenerator : IOtpCodeGenerator
{
    public string GenerateNumericCode(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentException("Otp length must be greater than zero.", nameof(length));
        }

        Span<char> chars = stackalloc char[length];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(chars);
    }
}
