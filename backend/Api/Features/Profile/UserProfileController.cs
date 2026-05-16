using System.Security.Claims;
using Api.Features.Profile.Dtos;
using Application.Features.Profile.Dtos;
using Application.Features.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Profile;

[Route("api/profile")]
[ApiController]
public sealed class UserProfileController : ControllerBase
{
    private readonly ProfileService _profileService;

    public UserProfileController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ProfileHttpResponse>> GetProfile(CancellationToken ct)
    {
        var profile = await _profileService.GetProfileAsync(GetAuthenticatedUserId(), ct);

        return Ok(ToHttpResponse(profile));
    }

    [Authorize]
    [HttpPatch]
    public async Task<ActionResult<ProfileHttpResponse>> UpdateProfile(
        [FromBody] UpdateProfileHttpRequest request,
        CancellationToken ct)
    {
        var profile = await _profileService.UpdateProfileAsync(
            new UpdateProfileRequest(
                GetAuthenticatedUserId(),
                request.DisplayName,
                request.AvatarUrl,
                request.CurrentPassword,
                request.NewPassword),
            ct);

        return Ok(ToHttpResponse(profile));
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

    private static ProfileHttpResponse ToHttpResponse(UserProfileResult profile)
    {
        return new ProfileHttpResponse(
            profile.UserId,
            profile.Email,
            profile.DisplayName,
            profile.AvatarUrl,
            profile.HasPassword);
    }
}
