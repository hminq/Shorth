using System;
using Application.Features.Links.Interfaces;
using Application.Features.Links.Dtos;
using Domain.Features.Links.Constants;
using Domain.Features.Links.Exceptions;

namespace Application.Features.Links.UseCases;

public class ResolveShortLinkUseCase
{
    private readonly ILinkRepository _linkRepository;
    private readonly ILinkCacheRepository _linkCacheRepository;
    private readonly IClickEventQueue _clickEventQueue;

    public ResolveShortLinkUseCase(ILinkRepository linkRepository, ILinkCacheRepository linkCacheRepository)
    {
        _linkRepository = linkRepository;
        _linkCacheRepository = linkCacheRepository;
    }

    public async Task<ResolveLinkResponse> ExecuteAsync(ResolveLinkRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ArgumentException("Slug is required.", nameof(request));
        }

        if (request.Slug.Length != SlugRules.SlugLength)
        {
            throw new ArgumentException($"Slug must be {SlugRules.SlugLength} characters long.", nameof(request));
        }

        // check cache first
        var cachedDestinationUrl = await _linkCacheRepository.GetDestinationUrlBySlugAsync(request.Slug, ct);

        // if cache hit, return destinationUrl
        if (cachedDestinationUrl != null)
        {
            return new ResolveLinkResponse(cachedDestinationUrl);
        }

        // check database if cache miss
        var link = await _linkRepository.GetBySlugAsync(request.Slug, ct);

        // slug found
        if (link != null)
        {
            // check if that link is expired or disable
            if (link.IsDisabled)
            {
                throw new InvalidLinkStateException("The link is disabled.");
            }

            if (link.ExpiresAt.HasValue && DateTime.UtcNow >= link.ExpiresAt.Value)
            {
                throw new InvalidLinkStateException("The link is expired.");
            }

            // set cache
            await _linkCacheRepository.SetDestinationUrlBySlugAsync(link.Slug, link.DestinationUrl, ct);
            
            return new ResolveLinkResponse(link.DestinationUrl);
        }

        // not found
        return new ResolveLinkResponse(null);
    }
}
