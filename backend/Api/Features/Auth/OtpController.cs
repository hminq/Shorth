using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Auth;

[Route("api/otp")]
[ApiController]
public sealed class OtpController : ControllerBase
{
    private readonly AuthService _authService;

    public OtpController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<ResendVerificationOtpHttpResponse>> ResendVerificationOtp(
        [FromBody] ResendVerificationOtpHttpRequest request,
        CancellationToken ct)
    {
        var serviceRequest = new ResendVerificationOtpRequest(request.Email);
        var resendResult = await _authService.ResendVerificationOtpAsync(serviceRequest, ct);
        var response = new ResendVerificationOtpHttpResponse(
            resendResult.Email,
            resendResult.RequiresEmailVerification);

        return Ok(response);
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<VerifyEmailOtpHttpResponse>> VerifyEmailOtp(
        [FromBody] VerifyEmailOtpHttpRequest request,
        CancellationToken ct)
    {
        var serviceRequest = new VerifyEmailOtpRequest(request.Email, request.OtpCode);
        var verifyResult = await _authService.VerifyEmailOtpAsync(serviceRequest, ct);
        var response = new VerifyEmailOtpHttpResponse(
            verifyResult.UserId,
            verifyResult.Email,
            verifyResult.EmailVerified);

        return Ok(response);
    }
}
