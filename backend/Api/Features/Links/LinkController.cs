using Api.Features.Links.Dtos;
using Application.Features.Links.Dtos;
using Application.Features.Links.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Links
{
    [Route("api/links")]
    [ApiController]
    public sealed class LinkController(
        LinkService linkService
        ) : ControllerBase
    {
        private readonly LinkService _linkService = linkService;

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
        private static CreateLinkRequest ToServiceRequest(CreateLinkHttpRequest request) {
            return new CreateLinkRequest(
                request.OwnerId,
                request.DestinationUrl,
                request.ExpiresAt
            );
        }

        private static ResolveLinkRequest ToServiceRequest(string slug)
        {
            return new ResolveLinkRequest(slug);
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
    }
}
