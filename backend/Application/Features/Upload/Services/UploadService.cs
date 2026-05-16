using Application.Features.Upload.Dtos;
using Application.Features.Upload.Interfaces;

namespace Application.Features.Upload.Services;

public sealed class UploadService
{
    private const long ProfileImageMaxSizeBytes = 1_048_576;
    private static readonly TimeSpan PresignTtl = TimeSpan.FromMinutes(5);
    private static readonly UploadPolicy ProfileImagePolicy = new(
        FileType: UploadFileType.ProfileImage,
        ObjectKeyPrefix: "profile-image",
        MaxSizeBytes: ProfileImageMaxSizeBytes,
        ExtensionsByContentType: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp"
        });

    private readonly IUploadPresignService _uploadPresignService;

    public UploadService(IUploadPresignService uploadPresignService)
    {
        _uploadPresignService = uploadPresignService;
    }

    public async Task<CreateUploadResult> CreateUploadAsync(CreateUploadRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(request));
        }

        var fileType = ParseFileType(request.FileType);
        var policy = GetPolicy(fileType);

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("File name is required.", nameof(request));
        }

        if (!policy.ExtensionsByContentType.TryGetValue(request.ContentType, out var extension))
        {
            throw new ArgumentException("Unsupported content type.", nameof(request));
        }

        if (request.FileSizeBytes <= 0)
        {
            throw new ArgumentException("File size must be positive.", nameof(request));
        }

        if (request.FileSizeBytes > policy.MaxSizeBytes)
        {
            throw new ArgumentException("Profile image must be 1 MB or smaller.", nameof(request));
        }

        var objectKey = $"{policy.ObjectKeyPrefix}/{Guid.NewGuid():N}{extension}";
        var expiresAt = DateTime.UtcNow.Add(PresignTtl);
        var presign = await _uploadPresignService.CreatePresignedPostAsync(
            new UploadPresignRequest(
                objectKey,
                request.ContentType,
                policy.MaxSizeBytes,
                expiresAt),
            ct);

        return new CreateUploadResult(
            presign.UploadUrl,
            presign.Fields,
            objectKey,
            presign.PublicUrl,
            policy.MaxSizeBytes,
            expiresAt);
    }

    private static UploadFileType ParseFileType(string fileType)
    {
        if (string.Equals(fileType, "profile_image", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileType, "profile-image", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileType, "avatar", StringComparison.OrdinalIgnoreCase))
        {
            return UploadFileType.ProfileImage;
        }

        throw new ArgumentException("Unsupported file type.", nameof(fileType));
    }

    private static UploadPolicy GetPolicy(UploadFileType fileType)
    {
        return fileType switch
        {
            UploadFileType.ProfileImage => ProfileImagePolicy,
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unsupported file type.")
        };
    }
}

internal sealed record UploadPolicy(
    UploadFileType FileType,
    string ObjectKeyPrefix,
    long MaxSizeBytes,
    IReadOnlyDictionary<string, string> ExtensionsByContentType);
