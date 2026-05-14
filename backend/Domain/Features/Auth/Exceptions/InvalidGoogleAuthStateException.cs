using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public sealed class InvalidGoogleAuthStateException : DomainException
{
    public InvalidGoogleAuthStateException(string message) : base(message)
    {
    }
}
