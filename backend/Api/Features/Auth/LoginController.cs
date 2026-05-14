using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Auth
{
    [Route("api/login")]
    [ApiController]
    public sealed class LoginController : ControllerBase
    {
        private readonly AuthService _authService;

        public LoginController(
            AuthService authService
        )
        {
            _authService = authService;
        }

        [HttpPost("local")]
        public async Task<ActionResult<LoginHttpResponse>> LocalLogin(
            [FromBody] LocalLoginHttpRequest request,
            CancellationToken ct)
        {
            var serviceRequest = ToServiceRequest(request);

            var loginResult = await _authService.LocalLoginAsync(serviceRequest, ct);
            var response = ToHttpResponse(loginResult);

            return Ok(response);
        }

        [HttpGet("google")]
        public async Task<ActionResult<GoogleLoginUrlHttpResponse>> GenerateGoogleLoginUrl(
            CancellationToken ct)
        {
            var result = await _authService.GenerateGoogleLoginUrlAsync(ct);
            var response = new GoogleLoginUrlHttpResponse(result.AuthorizationUrl);

            return Ok(response);
        }

        [HttpGet("~/api/oauth-google")]
        public async Task<ActionResult<LoginHttpResponse>> GoogleLoginCallback(
            [FromQuery] string code,
            [FromQuery] string? state,
            CancellationToken ct)
        {
            var loginResult = await _authService.GoogleLoginAsync(
                new GoogleLoginRequest(code, state),
                ct);
            var response = ToHttpResponse(loginResult);

            return Ok(response);
        }

        private static LocalLoginRequest ToServiceRequest(LocalLoginHttpRequest request)
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
