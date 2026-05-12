using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class VerificationOtpInactiveException : DomainException
{
    public VerificationOtpInactiveException() {}

    public VerificationOtpInactiveException(string message)
        : base(message)
    {
    }

    public VerificationOtpInactiveException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
