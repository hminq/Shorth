namespace Api.Features.Links.Dtos;

public sealed record GetUserLinksHttpResponse(
    int Page,
    int PageSize,
    bool HasNextPage,
    IReadOnlyList<UserLinkSummaryHttpResponse> Items);
