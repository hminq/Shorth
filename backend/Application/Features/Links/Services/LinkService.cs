using Application.Features.Links.Dtos;
using Application.Features.Links.Interfaces;
using Domain.Features.Links.Constants;
using Domain.Features.Links.Entities;
using Domain.Features.Links.Exceptions;

namespace Application.Features.Links.Services;

public sealed class LinkService
{
    private readonly ILinkRepository _linkRepository;
    private readonly ILinkCacheRepository _linkCacheRepository;
    private readonly IClickEventQueue _clickEventQueue;
    private readonly ISlugGenerator _slugGenerator;

    public LinkService(
        ILinkRepository linkRepository,
        ILinkCacheRepository linkCacheRepository,
        IClickEventQueue clickEventQueue,
        ISlugGenerator slugGenerator)
    {
        _linkRepository = linkRepository;
        _linkCacheRepository = linkCacheRepository;
        _clickEventQueue = clickEventQueue;
        _slugGenerator = slugGenerator;
    }

    public async Task<CreateLinkResult> CreateShortLinkAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DestinationUrl))
        {
            throw new ArgumentException("Destination url is required.", nameof(request));
        }

        var createdAt = DateTime.UtcNow;

        for (var attempt = 0; attempt < SlugRules.MaxGenerateRetries; attempt++)
        {
            var slug = _slugGenerator.Generate();

            var link = Link.Create(
                request.OwnerId,
                slug,
                request.DestinationUrl,
                createdAt,
                request.ExpiresAt
            );

            try
            {
                await _linkRepository.AddAsync(link, ct);

                return new CreateLinkResult(
                    link.Id,
                    link.OwnerId,
                    link.Slug,
                    link.DestinationUrl,
                    link.CreatedAt,
                    link.ExpiresAt
                );
            }
            catch (DuplicateSlugException) when (attempt < SlugRules.MaxGenerateRetries - 1)
            {
            }
        }

        throw new InvalidOperationException("Failed to generate a unique slug.");
    }

    public async Task<ResolveLinkResult> ResolveShortLinkAsync(ResolveLinkRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ArgumentException("Slug is required.", nameof(request));
        }

        if (request.Slug.Length != SlugRules.SlugLength)
        {
            throw new ArgumentException($"Slug must be {SlugRules.SlugLength} characters long.", nameof(request));
        }

        var cachedDestinationUrl = await _linkCacheRepository.GetDestinationUrlBySlugAsync(request.Slug, ct);

        if (cachedDestinationUrl != null)
        {
            return new ResolveLinkResult(cachedDestinationUrl);
        }

        var link = await _linkRepository.GetBySlugAsync(request.Slug, ct);

        if (link != null)
        {
            if (link.IsDisabled)
            {
                throw new InvalidLinkStateException("The link is disabled.");
            }

            if (link.ExpiresAt.HasValue && DateTime.UtcNow >= link.ExpiresAt.Value)
            {
                throw new InvalidLinkStateException("The link is expired.");
            }

            await _linkCacheRepository.SetDestinationUrlBySlugAsync(link.Slug, link.DestinationUrl, ct);

            return new ResolveLinkResult(link.DestinationUrl);
        }

        return new ResolveLinkResult(null);
    }
}
