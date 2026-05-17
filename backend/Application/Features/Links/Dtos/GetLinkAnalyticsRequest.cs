namespace Application.Features.Links.Dtos;

public sealed record GetLinkAnalyticsRequest(
    Guid UserId,
    Guid LinkId,
    DateOnly? From,
    DateOnly? To);
