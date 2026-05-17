namespace Application.Features.Links.Dtos;

public sealed record LinkCacheEntry(
    Guid LinkId,
    string DestinationUrl);
