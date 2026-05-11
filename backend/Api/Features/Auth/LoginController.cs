using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Auth
{
    [Route("api/login")]
    [ApiController]
    public sealed class LoginController : ControllerBase
    {
        private readonly LocalLoginUseCase _localLoginUseCase;

        public LoginController(
            LocalLoginUseCase localLogin
        )
        {
            _localLoginUseCase = localLogin;
        }

        [HttpPost("local")]
        public async Task<ActionResult<LoginHttpResponse>> LocalLogin(
            [FromBody] LocalLoginHttpRequest request,
            CancellationToken ct)
        {
            var useCaseRequest = ToUseCaseRequest(request);

            var loginResult = await _localLoginUseCase.ExecuteAsync(useCaseRequest, ct);
            var response = ToHttpResponse(loginResult);

            return Ok(response);
        }

        private LocalLoginRequest ToUseCaseRequest(LocalLoginHttpRequest request)
        {
            return new LocalLoginRequest(request.Email, request.Password);
        }

        private LoginHttpResponse ToHttpResponse(LoginResult result)
        {
            return new LoginHttpResponse(
                result.AccessToken,
                result.UserId,
                result.Email,
                result.DisplayName
            );
        }
    }
}
