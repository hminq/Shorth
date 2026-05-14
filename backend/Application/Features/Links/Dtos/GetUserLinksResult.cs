namespace Application.Features.Links.Dtos;

public sealed record GetUserLinksResult(
    int Page,
    int PageSize,
    bool HasNextPage,
    IReadOnlyList<UserLinkSummary> Items);
