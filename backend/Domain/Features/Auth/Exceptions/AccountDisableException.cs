using System;

using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class AccountDisableException : DomainException
{
    public AccountDisableException() {}

    public AccountDisableException(string message)
        : base(message)
    {
    }

    public AccountDisableException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
