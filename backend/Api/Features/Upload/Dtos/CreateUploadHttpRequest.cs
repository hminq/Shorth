using System.ComponentModel.DataAnnotations;

namespace Api.Features.Upload.Dtos;

public sealed class CreateUploadHttpRequest
{
    [Required]
    public string FileType { get; init; } = default!;

    [Required]
    [StringLength(180)]
    public string FileName { get; init; } = default!;

    [Required]
    public string ContentType { get; init; } = default!;

    [Range(1, long.MaxValue)]
    public long FileSizeBytes { get; init; }
}
