using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public sealed class GoogleEmailUnavailableException : DomainException
{
    public GoogleEmailUnavailableException(string message) : base(message)
    {
    }
}
