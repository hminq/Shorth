namespace Api.Features.Upload.Dtos;

public sealed record CreateUploadHttpResponse(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Fields,
    string ObjectKey,
    string PublicUrl,
    long MaxSizeBytes,
    DateTime ExpiresAt);
