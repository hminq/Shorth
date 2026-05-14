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

    [HttpPost]
    public async Task<ActionResult<RegisterHttpResponse>> LocalRegister(
        [FromBody] LocalRegisterHttpRequest request,
        CancellationToken ct)
    {
        var serviceRequest = ToServiceRequest(request);
        var registerResult = await _authService.LocalRegisterAsync(serviceRequest, ct);
        var response = ToHttpResponse(registerResult);

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
