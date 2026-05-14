using System.ComponentModel.DataAnnotations;

namespace Api.Features.Links.Dtos;

public sealed class CreateLinkHttpRequest
{
    [Required]
    [Url]
    public string DestinationUrl { get; init; } = default!;

    public DateTime? ExpiresAt { get; init; }
}
