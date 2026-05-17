using Api.Features.Links.Dtos;
using Application.Features.Links.Dtos;
using Application.Features.Links.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

namespace Api.Features.Links
{
    [Route("api/links")]
    [ApiController]
    public sealed class LinkController(
        LinkService linkService
        ) : ControllerBase
    {
        private const int UserLinksPageSize = 10;

        private readonly LinkService _linkService = linkService;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<GetUserLinksHttpResponse>> GetUserLinks(
            [FromQuery] string? page,
            CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var result = await _linkService.GetUserLinksAsync(
                new GetUserLinksRequest(userId, ParsePage(page), UserLinksPageSize),
                ct);

            return Ok(ToHttpResponse(result));
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<LinkDetailHttpResponse>> GetLinkDetail(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var result = await _linkService.GetLinkDetailAsync(
                new GetLinkDetailRequest(userId, id),
                ct);

            if (result is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Link not found",
                    Detail = "The link does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(ToHttpResponse(result));
        }

        [Authorize]
        [HttpGet("{id:guid}/analytics")]
        public async Task<ActionResult<LinkAnalyticsHttpResponse>> GetLinkAnalytics(
            [FromRoute] Guid id,
            [FromQuery] string? from,
            [FromQuery] string? to,
            CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var result = await _linkService.GetLinkAnalyticsAsync(
                new GetLinkAnalyticsRequest(userId, id, ParseDate(from), ParseDate(to)),
                ct);

            if (result is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Link not found",
                    Detail = "The link does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(ToHttpResponse(result));
        }

        [HttpPost]
        public async Task<ActionResult<CreateLinkHttpResponse>> CreateNewLink(
            [FromBody] CreateLinkHttpRequest request, 
            CancellationToken ct
            )
        {
            var serviceRequest = ToServiceRequest(request);

            var result = await _linkService.CreateShortLinkAsync(serviceRequest, ct);

            var response = ToHttpResponse(result);

            return Created($"/api/links/{result.Id}", response);
        }

        [HttpGet("/{slug}")]
        public async Task<IActionResult> ResolveLink(
            [FromRoute] string slug,
            CancellationToken ct)
        {
            var serviceRequest = ToServiceRequestWithMetadata(slug);

            var result = await _linkService.ResolveShortLinkAsync(serviceRequest, ct);

            if (result.DestinationUrl is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Link not found",
                    Detail = "The short link does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Redirect(result.DestinationUrl);
        }

        // Mapper methods
        private CreateLinkRequest ToServiceRequest(CreateLinkHttpRequest request) {
            return new CreateLinkRequest(
                GetOptionalCurrentUserId(),
                request.DestinationUrl,
                request.ExpiresAt,
                request.CaptchaToken
            );
        }

        private ResolveLinkRequest ToServiceRequestWithMetadata(string slug)
        {
            return new ResolveLinkRequest(
                slug,
                NormalizeHeader(Request.Headers["User-Agent"].ToString(), 512),
                NormalizeHeader(Request.Headers["Referer"].ToString(), 512),
                HashIpAddress(GetClientIpAddress()),
                NormalizeCountryCode(Request.Headers["CF-IPCountry"].ToString()));
        }

        private string? GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private static string? HashIpAddress(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return null;
            }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string? NormalizeHeader(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static string? NormalizeCountryCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim().ToUpperInvariant();
            return trimmed.Length == 2 && trimmed.All(static c => c is >= 'A' and <= 'Z') ? trimmed : null;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("user_id");
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("Authenticated user id is missing.");
            }

            return userId;
        }

        private Guid? GetOptionalCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return GetCurrentUserId();
        }

        private static int ParsePage(string? page)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                return 1;
            }

            if (!int.TryParse(
                    page,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var parsed) || parsed < 1)
            {
                throw new ArgumentException("Page must be a positive integer.", nameof(page));
            }

            return parsed;
        }

        private static DateOnly? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!DateOnly.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                throw new ArgumentException("Date must use yyyy-MM-dd format.", nameof(value));
            }

            return parsed;
        }

        private static CreateLinkHttpResponse ToHttpResponse(CreateLinkResult result) {
            return new CreateLinkHttpResponse(
                result.Id,
                result.Slug,
                result.DestinationUrl,
                result.CreatedAt,
                result.ExpiresAt
            );
        }

        private static GetUserLinksHttpResponse ToHttpResponse(GetUserLinksResult result)
        {
            return new GetUserLinksHttpResponse(
                result.Page,
                result.PageSize,
                result.HasNextPage,
                result.Items
                    .Select(item => new UserLinkSummaryHttpResponse(
                        item.Id,
                        item.Slug,
                        item.DestinationUrl,
                        item.ClickCount,
                        item.LastClickedAt,
                        item.CreatedAt,
                        item.ExpiresAt,
                        item.IsDisabled))
                    .ToList());
        }

        private static LinkDetailHttpResponse ToHttpResponse(LinkDetailResult result)
        {
            return new LinkDetailHttpResponse(
                result.Id,
                result.Slug,
                result.DestinationUrl,
                result.ClickCount,
                result.LastClickedAt,
                result.CreatedAt,
                result.UpdatedAt,
                result.ExpiresAt,
                result.IsDisabled);
        }

        private static LinkAnalyticsHttpResponse ToHttpResponse(LinkAnalyticsResult result)
        {
            return new LinkAnalyticsHttpResponse(
                result.LinkId,
                result.TotalClicks,
                result.LastClickedAt,
                result.From,
                result.To,
                result.Daily
                    .Select(item => new LinkDailyAnalyticsHttpResponse(
                        item.Date,
                        item.Clicks,
                        item.UniqueVisitors))
                    .ToList(),
                result.TopCountries
                    .Select(item => new LinkCountryAnalyticsHttpResponse(
                        item.CountryCode,
                        item.Clicks,
                        item.Percent))
                    .ToList());
        }
    }
}
