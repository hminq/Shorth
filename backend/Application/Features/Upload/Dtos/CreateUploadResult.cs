namespace Application.Features.Upload.Dtos;

public sealed record CreateUploadResult(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Fields,
    string ObjectKey,
    string PublicUrl,
    long MaxSizeBytes,
    DateTime ExpiresAt);
