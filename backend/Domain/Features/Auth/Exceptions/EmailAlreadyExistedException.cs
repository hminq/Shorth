using System;
using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class EmailAlreadyExistedException : DomainException
{
    public EmailAlreadyExistedException() {}

    public EmailAlreadyExistedException(string message)
        : base(message)
    {
    }

    public EmailAlreadyExistedException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
