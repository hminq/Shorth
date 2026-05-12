using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class OtpResendTooSoonException : DomainException
{
    public OtpResendTooSoonException() {}

    public OtpResendTooSoonException(string message)
        : base(message)
    {
    }

    public OtpResendTooSoonException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
