using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Auth;

[Route("api/register")]
[ApiController]
public sealed class RegisterController : ControllerBase
{
    private readonly LocalRegisterUseCase _localRegisterUseCase;

    public RegisterController(LocalRegisterUseCase localRegisterUseCase)
    {
        _localRegisterUseCase = localRegisterUseCase;
    }

    [HttpPost("local")]
    public async Task<ActionResult<RegisterHttpResponse>> LocalRegister(
        [FromBody] LocalRegisterHttpRequest request,
        CancellationToken ct)
    {
        var useCaseRequest = ToUseCaseRequest(request);
        var registerResult = await _localRegisterUseCase.ExecuteAsync(useCaseRequest, ct);
        var response = ToHttpResponse(registerResult);

        return Ok(response);
    }

    private static LocalRegisterRequest ToUseCaseRequest(LocalRegisterHttpRequest request)
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
