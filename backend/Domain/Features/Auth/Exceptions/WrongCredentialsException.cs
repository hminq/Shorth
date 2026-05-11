using System;

using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class WrongCredentialsException : DomainException
{
    public WrongCredentialsException() {}

    public WrongCredentialsException(string message)
        : base(message)
    {
    }

    public WrongCredentialsException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
