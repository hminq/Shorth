using System;

using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class InvalidOtpStateException : DomainException
{
    public InvalidOtpStateException() {}

    public InvalidOtpStateException(string message)
        : base(message)
    {
    }

    public InvalidOtpStateException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
