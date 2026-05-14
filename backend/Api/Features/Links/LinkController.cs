using Api.Features.Links.Dtos;
using Application.Features.Links.Dtos;
using Application.Features.Links.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var serviceRequest = ToServiceRequest(slug);

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
                request.ExpiresAt
            );
        }

        private static ResolveLinkRequest ToServiceRequest(string slug)
        {
            return new ResolveLinkRequest(slug);
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
            return int.TryParse(page, out var parsed) && parsed > 0 ? parsed : 1;
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
    }
}
