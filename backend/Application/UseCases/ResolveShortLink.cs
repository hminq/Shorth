using System;
using Application.Abstractions;
using Application.Dtos;
using Domain.Constants;
using Domain.Exceptions;

namespace Application.UseCases;

public class ResolveShortLink
{
    private readonly ILinkRepository _linkRepository;
    private readonly ILinkCacheRepository _linkCacheRepository;

    public ResolveShortLink(ILinkRepository linkRepository, ILinkCacheRepository linkCacheRepository)
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
