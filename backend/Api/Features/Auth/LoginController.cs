using Api.Configurations;
using Api.Features.Auth.Dtos;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Services;
using Domain.Features.Auth.Exceptions;
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
            SetAuthCookies(loginResult);
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
            try
            {
                var loginResult = await _authService.GoogleLoginAsync(
                    new GoogleLoginRequest(code, state),
                    ct);
                SetAuthCookies(loginResult);

                return Redirect($"{_authCookieOptions.WebBaseUrl}/auth/callback");
            }
            catch (GoogleEmailUnavailableException)
            {
                return RedirectWithGoogleError(
                    "google_email_unverified",
                    "Your Google account email is not verified. Verify it with Google or sign up with email instead.");
            }
            catch (InvalidGoogleAuthStateException)
            {
                return RedirectWithGoogleError(
                    "google_state_invalid",
                    "Could not finish Google sign-in. Please try again.");
            }
        }

        [HttpPost("~/api/auth/refresh")]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            Request.Cookies.TryGetValue(_authCookieOptions.RefreshCookieName, out var refreshToken);
            var loginResult = await _authService.RefreshAsync(refreshToken, ct);
            SetAuthCookies(loginResult);

            return NoContent();
        }

        [HttpPost("~/api/logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            Request.Cookies.TryGetValue(_authCookieOptions.RefreshCookieName, out var refreshToken);
            await _authService.LogoutAsync(refreshToken, ct);
            ClearAuthCookies();

            return NoContent();
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

        private void SetAuthCookies(LoginResult loginResult)
        {
            SetCookie(
                _authCookieOptions.CookieName,
                loginResult.AccessToken,
                DateTimeOffset.UtcNow.AddMinutes(_authCookieOptions.AccessTokenTtlMinutes));
            SetCookie(
                _authCookieOptions.RefreshCookieName,
                loginResult.RefreshToken,
                DateTimeOffset.UtcNow.AddDays(_authCookieOptions.RefreshTokenTtlDays));
        }

        private void SetCookie(string name, string value, DateTimeOffset expires)
        {
            Response.Cookies.Append(
                name,
                value,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _environment.IsProduction(),
                    SameSite = _environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
                    Path = "/",
                    Expires = expires
                });
        }

        private void ClearAuthCookies()
        {
            ClearCookie(_authCookieOptions.CookieName);
            ClearCookie(_authCookieOptions.RefreshCookieName);
        }

        private void ClearCookie(string name)
        {
            Response.Cookies.Delete(
                name,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _environment.IsProduction(),
                    SameSite = _environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
                    Path = "/"
                });
        }

        private RedirectResult RedirectWithGoogleError(string code, string message)
        {
            return Redirect(
                $"{_authCookieOptions.WebBaseUrl}/auth/callback?error={Uri.EscapeDataString(code)}&message={Uri.EscapeDataString(message)}");
        }
    }
}
