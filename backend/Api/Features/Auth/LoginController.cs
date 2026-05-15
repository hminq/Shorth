using Api.Configurations;
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
        private readonly AuthCookieOptions _authCookieOptions;
        private readonly IWebHostEnvironment _environment;

        public LoginController(
            AuthService authService,
            AuthCookieOptions authCookieOptions,
            IWebHostEnvironment environment
        )
        {
            _authService = authService;
            _authCookieOptions = authCookieOptions;
            _environment = environment;
        }

        [HttpPost("local")]
        public async Task<ActionResult<LoginHttpResponse>> LocalLogin(
            [FromBody] LocalLoginHttpRequest request,
            CancellationToken ct)
        {
            var serviceRequest = ToServiceRequest(request);

            var loginResult = await _authService.LocalLoginAsync(serviceRequest, ct);
            SetAccessTokenCookie(loginResult.AccessToken);
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
            SetAccessTokenCookie(loginResult.AccessToken);

            return Redirect($"{_authCookieOptions.WebBaseUrl}/auth/callback");
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

        private void SetAccessTokenCookie(string accessToken)
        {
            Response.Cookies.Append(
                _authCookieOptions.CookieName,
                accessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _environment.IsProduction(),
                    SameSite = _environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(_authCookieOptions.AccessTokenTtlMinutes)
                });
        }
    }
}
