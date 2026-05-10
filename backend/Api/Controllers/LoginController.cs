using Api.Dtos;
using Application.Dtos;
using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LocalLogin _localLogin;

        public LoginController(
            LocalLogin localLogin
        )
        {
            _localLogin = localLogin;
        }

        [HttpPost("local")]
        public async Task<ActionResult<LoginHttpResponse>> LocalLogin(
            [FromBody] LocalLoginHttpRequest request,
            CancellationToken ct)
        {
            var useCaseRequest = ToUseCaseRequest(request);

            var loginResult = await _localLogin.ExecuteAsync(useCaseRequest, ct);
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
