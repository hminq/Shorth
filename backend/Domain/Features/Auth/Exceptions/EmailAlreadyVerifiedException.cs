using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class EmailAlreadyVerifiedException : DomainException
{
    public EmailAlreadyVerifiedException() {}

    public EmailAlreadyVerifiedException(string message)
        : base(message)
    {
    }

    public EmailAlreadyVerifiedException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
