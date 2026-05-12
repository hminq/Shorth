namespace Domain.Features.Auth.Exceptions;

public sealed class EmailVerificationRequiredException : Exception
{
    public EmailVerificationRequiredException(string message) : base(message)
    {
    }
}
