namespace Application.Features.Upload.Dtos;

public sealed record CreateUploadRequest(
    Guid UserId,
    string FileType,
    string FileName,
    string ContentType,
    long FileSizeBytes);
