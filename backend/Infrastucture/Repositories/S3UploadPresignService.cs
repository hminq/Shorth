using Amazon.S3;
using Amazon.S3.Model;
using Application.Features.Upload.Dtos;
using Application.Features.Upload.Interfaces;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class S3UploadPresignService : IUploadPresignService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;

    public S3UploadPresignService(IAmazonS3 s3Client, S3Options options)
    {
        _s3Client = s3Client;
        _options = options;
    }

    public async Task<UploadPresignResult> CreatePresignedPostAsync(
        UploadPresignRequest request,
        CancellationToken ct = default)
    {
        var presignRequest = new CreatePresignedPostRequest
        {
            BucketName = _options.BucketName,
            Key = request.ObjectKey,
            Expires = request.ExpiresAt,
            Fields = new Dictionary<string, string>
            {
                ["key"] = request.ObjectKey,
                ["Content-Type"] = request.ContentType
            },
            Conditions = new List<S3PostCondition>
            {
                S3PostCondition.ExactMatch("key", request.ObjectKey),
                S3PostCondition.ExactMatch("Content-Type", request.ContentType),
                S3PostCondition.ContentLengthRange(1, request.MaxSizeBytes)
            }
        };

        var response = await _s3Client.CreatePresignedPostAsync(presignRequest);
        var fields = response.Fields.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);

        return new UploadPresignResult(
            response.Url,
            fields,
            $"{_options.PublicBaseUrl.TrimEnd('/')}/{request.ObjectKey}");
    }
}
