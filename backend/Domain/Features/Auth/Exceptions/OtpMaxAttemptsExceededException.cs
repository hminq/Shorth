using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class OtpMaxAttemptsExceededException : DomainException
{
    public OtpMaxAttemptsExceededException() {}

    public OtpMaxAttemptsExceededException(string message)
        : base(message)
    {
    }

    public OtpMaxAttemptsExceededException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
