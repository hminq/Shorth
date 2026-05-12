using Domain.Common.Exceptions;

namespace Domain.Features.Auth.Exceptions;

public class WrongOtpException : DomainException
{
    public WrongOtpException() {}

    public WrongOtpException(string message)
        : base(message)
    {
    }

    public WrongOtpException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
