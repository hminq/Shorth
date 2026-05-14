namespace Application.Features.Links.Dtos;

public sealed record GetUserLinksRequest(
    Guid UserId,
    int Page,
    int PageSize);
