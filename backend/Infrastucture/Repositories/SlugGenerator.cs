using System;
using Application.Abstractions;
using Domain.Constants;
using System.Security.Cryptography;

namespace Infrastucture.Repositories;

public class SlugGenerator : ISlugGenerator
{
    public string Generate()
    {
        var chars = new char[SlugRules.SlugLength];

        for (var i = 0; i < chars.Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(SlugRules.SlugCharacterSet.Length);
            chars[i] = SlugRules.SlugCharacterSet[index];
        }

        return new string(chars);
    }
}
