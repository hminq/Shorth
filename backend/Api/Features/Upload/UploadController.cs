using System.Security.Claims;
using Api.Features.Upload.Dtos;
using Application.Features.Upload.Dtos;
using Application.Features.Upload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Upload;

[Route("api/upload")]
[ApiController]
public sealed class UploadController : ControllerBase
{
    private readonly UploadService _uploadService;

    public UploadController(UploadService uploadService)
    {
        _uploadService = uploadService;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CreateUploadHttpResponse>> CreateUpload(
        [FromBody] CreateUploadHttpRequest request,
        CancellationToken ct)
    {
        var result = await _uploadService.CreateUploadAsync(
            new CreateUploadRequest(
                GetAuthenticatedUserId(),
                request.FileType,
                request.FileName,
                request.ContentType,
                request.FileSizeBytes),
            ct);

        return Ok(new CreateUploadHttpResponse(
            result.UploadUrl,
            result.Fields,
            result.ObjectKey,
            result.PublicUrl,
            result.MaxSizeBytes,
            result.ExpiresAt));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }
}
