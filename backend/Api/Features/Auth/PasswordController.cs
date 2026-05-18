using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Auth;

[ApiController]
public sealed class PasswordController : ControllerBase
{
    private const string ForgotPasswordMessage =
        "A reset code has been sent.";

    private readonly AuthService _authService;

    public PasswordController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("api/forgot-password")]
    public async Task<ActionResult<ForgotPasswordHttpResponse>> ForgotPassword(
        [FromBody] ForgotPasswordHttpRequest request,
        CancellationToken ct)
    {
        var result = await _authService.ForgotPasswordAsync(
            new ForgotPasswordRequest(request.Email, HttpContext.Connection.RemoteIpAddress?.ToString()),
            ct);

        return Ok(new ForgotPasswordHttpResponse(result.Email, ForgotPasswordMessage));
    }

    [HttpPost("api/password-reset/verify")]
    public async Task<ActionResult<VerifyPasswordResetHttpResponse>> VerifyPasswordReset(
        [FromBody] VerifyPasswordResetHttpRequest request,
        CancellationToken ct)
    {
        var result = await _authService.VerifyPasswordResetAsync(
            new VerifyPasswordResetRequest(request.Email, request.OtpCode),
            ct);

        return Ok(new VerifyPasswordResetHttpResponse(result.Email, result.ResetToken));
    }

    [HttpPost("api/password-reset/complete")]
    public async Task<ActionResult<PasswordResetHttpResponse>> CompletePasswordReset(
        [FromBody] CompletePasswordResetHttpRequest request,
        CancellationToken ct)
    {
        var result = await _authService.CompletePasswordResetAsync(
            new CompletePasswordResetRequest(request.ResetToken, request.NewPassword),
            ct);

        return Ok(new PasswordResetHttpResponse(result.Email, result.PasswordReset));
    }
}
