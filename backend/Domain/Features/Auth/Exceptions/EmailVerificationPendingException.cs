using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class EmailVerificationPendingException : DomainException
{
    public EmailVerificationPendingException() {}

    public EmailVerificationPendingException(string message)
        : base(message)
    {
    }

    public EmailVerificationPendingException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
