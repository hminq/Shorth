using System;

namespace Domain.Exceptions;

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
