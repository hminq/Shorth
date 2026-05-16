namespace Application.Features.Upload.Dtos;

public sealed record UploadPresignRequest(
    string ObjectKey,
    string ContentType,
    long MaxSizeBytes,
    DateTime ExpiresAt);
