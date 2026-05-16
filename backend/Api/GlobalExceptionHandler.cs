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
                Detail = "This account is disabled.",
                Status = StatusCodes.Status403Forbidden
            },
            WrongCredentialsException => CreateWrongCredentialsProblem(httpContext),
            AlternateSignInRequiredException => new ProblemDetails
            {
                Title = "Alternate sign-in required",
                Detail = "Use the sign-in method already linked to this email address.",
                Status = StatusCodes.Status409Conflict
            },
            EmailVerificationPendingException => new ProblemDetails
            {
                Title = "Email verification pending",
                Detail = "A verification code has already been sent. Please check your email.",
                Status = StatusCodes.Status409Conflict
            },
            EmailVerificationRequiredException => new ProblemDetails
            {
                Title = "Email verification required",
                Detail = "Verify your email before continuing.",
                Status = StatusCodes.Status409Conflict
            },
            EmailVerificationNotPendingException => new ProblemDetails
            {
                Title = "Email verification not pending",
                Detail = "There is no active email verification for this account.",
                Status = StatusCodes.Status409Conflict
            },
            EmailAlreadyVerifiedException => new ProblemDetails
            {
                Title = "Email already verified",
                Detail = "This email address is already verified.",
                Status = StatusCodes.Status409Conflict
            },
            EmailAlreadyExistedException => new ProblemDetails
            {
                Title = "Email already exists",
                Detail = "This email address already has an account.",
                Status = StatusCodes.Status409Conflict
            },
            InvalidGoogleAuthStateException => new ProblemDetails
            {
                Title = "Invalid Google auth state",
                Detail = "Could not finish Google sign-in. Please try again.",
                Status = StatusCodes.Status400BadRequest
            },
            GoogleEmailUnavailableException => new ProblemDetails
            {
                Title = "Google email unavailable",
                Detail = "Google did not provide a verified email address.",
                Status = StatusCodes.Status409Conflict
            },
            OtpResendTooSoonException => new ProblemDetails
            {
                Title = "Otp resend too soon",
                Detail = "Please wait before requesting another code.",
                Status = StatusCodes.Status429TooManyRequests
            },
            VerificationOtpInactiveException => new ProblemDetails
            {
                Title = "Verification code inactive",
                Detail = "This verification code is no longer valid. Request a new one.",
                Status = StatusCodes.Status409Conflict
            },
            OtpMaxAttemptsExceededException => new ProblemDetails
            {
                Title = "Otp max attempts exceeded",
                Detail = "Too many attempts. Request a new verification code.",
                Status = StatusCodes.Status409Conflict
            },
            WrongOtpException => new ProblemDetails
            {
                Title = "Wrong otp",
                Detail = "The verification code is incorrect.",
                Status = StatusCodes.Status400BadRequest
            },
            DomainException => new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Check the request and try again.",
                Status = StatusCodes.Status400BadRequest
            },
            ArgumentException => new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Check the fields and try again.",
                Status = StatusCodes.Status400BadRequest
            },
            InvalidOperationException => new ProblemDetails
            {
                Title = "Operation failed",
                Detail = "Could not complete the request. Please try again.",
                Status = StatusCodes.Status400BadRequest
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Title = "Invalid session",
                Detail = "Please sign in again to continue.",
                Status = StatusCodes.Status401Unauthorized
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

    private static ProblemDetails CreateWrongCredentialsProblem(HttpContext httpContext)
    {
        var path = httpContext.Request.Path;
        if (path.StartsWithSegments("/api/login"))
        {
            return new ProblemDetails
            {
                Title = "Sign-in failed",
                Detail = "Email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            };
        }

        return new ProblemDetails
        {
            Title = "Current password incorrect",
            Detail = "Current password is incorrect.",
            Status = StatusCodes.Status401Unauthorized
        };
    }
}
