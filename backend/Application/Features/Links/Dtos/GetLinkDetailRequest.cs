namespace Application.Features.Links.Dtos;

public sealed record GetLinkDetailRequest(
    Guid UserId,
    Guid LinkId);
