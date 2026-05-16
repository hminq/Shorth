namespace Application.Features.Upload.Dtos;

public sealed record UploadPresignResult(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Fields,
    string PublicUrl);
