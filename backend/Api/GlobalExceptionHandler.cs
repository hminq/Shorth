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
            EmailAlreadyExistedException => new ProblemDetails
            {
                Title = "Email already exists",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
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
