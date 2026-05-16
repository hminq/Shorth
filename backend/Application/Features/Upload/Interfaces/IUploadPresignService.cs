using Application.Features.Upload.Dtos;

namespace Application.Features.Upload.Interfaces;

public interface IUploadPresignService
{
    Task<UploadPresignResult> CreatePresignedPostAsync(UploadPresignRequest request, CancellationToken ct = default);
}
