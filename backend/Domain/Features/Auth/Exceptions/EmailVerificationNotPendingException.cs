using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class EmailVerificationNotPendingException : DomainException
{
    public EmailVerificationNotPendingException() {}

    public EmailVerificationNotPendingException(string message)
        : base(message)
    {
    }

    public EmailVerificationNotPendingException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
