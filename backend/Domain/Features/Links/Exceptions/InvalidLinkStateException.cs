using System;

using Domain.Common.Exceptions;

namespace Domain.Features.Links.Exceptions;

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
