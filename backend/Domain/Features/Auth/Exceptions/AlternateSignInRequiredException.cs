using System;

using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class AlternateSignInRequiredException : DomainException
{
    public AlternateSignInRequiredException() {}

    public AlternateSignInRequiredException(string message)
        : base(message)
    {
    }

    public AlternateSignInRequiredException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
