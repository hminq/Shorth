using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public sealed class CreateLinkHttpRequest
{
    [Required]
    [Url]
    public string DestinationUrl { get; init; } = default!;

    public Guid? OwnerId { get; init; }

    public DateTime? ExpiresAt { get; init; }
}
