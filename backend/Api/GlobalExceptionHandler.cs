using Domain.Common.Exceptions;
using Domain.Features.Auth.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception occurred.");

        var problemDetails = exception switch
        {
            AccountDisableException => new ProblemDetails
            {
                Title = "Account disabled",
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden
            },
            WrongCredentialsException => new ProblemDetails
            {
                Title = "Wrong credentials",
                Detail = exception.Message,
                Status = StatusCodes.Status401Unauthorized
            },
            AlternateSignInRequiredException => new ProblemDetails
            {
                Title = "Alternate sign-in required",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            EmailVerificationPendingException => new ProblemDetails
            {
                Title = "Email verification pending",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            EmailVerificationRequiredException => new ProblemDetails
            {
                Title = "Email verification required",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            EmailVerificationNotPendingException => new ProblemDetails
            {
                Title = "Email verification not pending",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            EmailAlreadyVerifiedException => new ProblemDetails
            {
                Title = "Email already verified",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            EmailAlreadyExistedException => new ProblemDetails
            {
                Title = "Email already exists",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            OtpResendTooSoonException => new ProblemDetails
            {
                Title = "Otp resend too soon",
                Detail = exception.Message,
                Status = StatusCodes.Status429TooManyRequests
            },
            VerificationOtpInactiveException => new ProblemDetails
            {
                Title = "Verification code inactive",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            OtpMaxAttemptsExceededException => new ProblemDetails
            {
                Title = "Otp max attempts exceeded",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            WrongOtpException => new ProblemDetails
            {
                Title = "Wrong otp",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            },
            DomainException => new ProblemDetails
            {
                Title = "Business rule violation",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            },
            ArgumentException => new ProblemDetails
            {
                Title = "Invalid request",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            },
            InvalidOperationException => new ProblemDetails
            {
                Title = "Operation failed",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            },
            _ => new ProblemDetails
            {
                Title = "Server error",
                Detail = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        return true;
    }
}
