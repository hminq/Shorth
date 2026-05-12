using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Auth;

[Route("api/register")]
[ApiController]
public sealed class RegisterController : ControllerBase
{
    private readonly AuthService _authService;

    public RegisterController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("local")]
    public async Task<ActionResult<RegisterHttpResponse>> LocalRegister(
        [FromBody] LocalRegisterHttpRequest request,
        CancellationToken ct)
    {
        var serviceRequest = ToServiceRequest(request);
        var registerResult = await _authService.LocalRegisterAsync(serviceRequest, ct);
        var response = ToHttpResponse(registerResult);

        return Ok(response);
    }

    [HttpPost("local/resend-verification-otp")]
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

    [HttpPost("local/verify-email-otp")]
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

    private static LocalRegisterRequest ToServiceRequest(LocalRegisterHttpRequest request)
    {
        return new LocalRegisterRequest(
            request.Email,
            request.Password,
            request.DisplayName);
    }

    private static RegisterHttpResponse ToHttpResponse(RegisterResult result)
    {
        return new RegisterHttpResponse(
            result.UserId,
            result.Email,
            result.DisplayName,
            result.RequiresEmailVerification);
    }
}
