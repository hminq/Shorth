using System;

namespace Domain.Exceptions;

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
