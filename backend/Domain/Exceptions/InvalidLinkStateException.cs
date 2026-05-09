using System;

namespace Domain.Exceptions;

public class InvalidLinkStateException : DomainException
{
    public InvalidLinkStateException() {}

    public InvalidLinkStateException(string message)
        : base(message)
    {
    }

    public InvalidLinkStateException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
