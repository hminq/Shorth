using Api.Features.Links.Dtos;
using Application.Features.Links.Dtos;
using Application.Features.Links.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Links
{
    [Route("api/links")]
    [ApiController]
    public sealed class LinkController(
        CreateShortLinkUseCase createShortLink,
        ResolveShortLinkUseCase resolveShortLink
        ) : ControllerBase
    {
        private readonly CreateShortLinkUseCase _createShortLink = createShortLink;
        private readonly ResolveShortLinkUseCase _resolveShortLink = resolveShortLink;

        [HttpPost]
        public async Task<ActionResult<CreateLinkHttpResponse>> CreateNewLink(
            [FromBody] CreateLinkHttpRequest request, 
            CancellationToken ct
            )
        {
            var useCaseRequest = ToUseCaseRequest(request);

            var result = await _createShortLink.ExecuteAsync(useCaseRequest, ct);

            var response = ToHttpResponse(result);

            return Created($"/api/links/{result.Id}", response);
        }

        [HttpGet("/{slug}")]
        public async Task<IActionResult> ResolveLink(
            [FromRoute] string slug,
            CancellationToken ct)
        {
            var useCaseRequest = ToUseCaseRequest(slug);

            var result = await _resolveShortLink.ExecuteAsync(useCaseRequest, ct);

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
        private static CreateLinkRequest ToUseCaseRequest(CreateLinkHttpRequest request) {
            return new CreateLinkRequest(
                request.OwnerId,
                request.DestinationUrl,
                request.ExpiresAt
            );
        }

        private static ResolveLinkRequest ToUseCaseRequest(string slug)
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
