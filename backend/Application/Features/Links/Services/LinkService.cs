using Application.Features.Links.Dtos;
using Application.Features.Links.Interfaces;
using Domain.Features.Links.Constants;
using Domain.Features.Links.Entities;
using Domain.Features.Links.Exceptions;

namespace Application.Features.Links.Services;

public sealed class LinkService
{
    private const int MaxPageSize = 10;
    private const int DefaultAnalyticsWindowDays = 30;
    private const int MaxAnalyticsWindowDays = 366;
    private const int TopCountriesLimit = 3;

    private readonly ILinkRepository _linkRepository;
    private readonly ILinkClickEventRepository _linkClickEventRepository;
    private readonly ILinkCacheRepository _linkCacheRepository;
    private readonly IClickEventQueue _clickEventQueue;
    private readonly ISlugGenerator _slugGenerator;
    private readonly ICaptchaVerifier _captchaVerifier;

    public LinkService(
        ILinkRepository linkRepository,
        ILinkClickEventRepository linkClickEventRepository,
        ILinkCacheRepository linkCacheRepository,
        IClickEventQueue clickEventQueue,
        ISlugGenerator slugGenerator,
        ICaptchaVerifier captchaVerifier)
    {
        _linkRepository = linkRepository;
        _linkClickEventRepository = linkClickEventRepository;
        _linkCacheRepository = linkCacheRepository;
        _clickEventQueue = clickEventQueue;
        _slugGenerator = slugGenerator;
        _captchaVerifier = captchaVerifier;
    }

    public async Task<CreateLinkResult> CreateShortLinkAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DestinationUrl))
        {
            throw new ArgumentException("Destination url is required.", nameof(request));
        }

        if (request.OwnerId is null)
        {
            if (string.IsNullOrWhiteSpace(request.CaptchaToken))
            {
                throw new ArgumentException("Human verification is required.", nameof(request));
            }

            if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, ct))
            {
                throw new ArgumentException("Human verification failed. Please try again.", nameof(request));
            }
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

    public async Task<GetUserLinksResult> GetUserLinksAsync(GetUserLinksRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(request));
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? MaxPageSize : Math.Min(request.PageSize, MaxPageSize);
        var skip = (page - 1) * pageSize;

        var links = await _linkRepository.GetByOwnerIdAsync(
            request.UserId,
            skip,
            pageSize + 1,
            ct);
        var hasNextPage = links.Count > pageSize;
        var items = links
            .Take(pageSize)
            .Select(link => new UserLinkSummary(
                link.Id,
                link.Slug,
                link.DestinationUrl,
                link.ClickCount,
                link.LastClickedAt,
                link.CreatedAt,
                link.ExpiresAt,
                link.IsDisabled))
            .ToList();

        return new GetUserLinksResult(
            page,
            pageSize,
            hasNextPage,
            items);
    }

    public async Task<LinkDetailResult?> GetLinkDetailAsync(GetLinkDetailRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(request));
        }

        if (request.LinkId == Guid.Empty)
        {
            throw new ArgumentException("Link id is required.", nameof(request));
        }

        var link = await _linkRepository.GetByIdAsync(request.LinkId, ct);
        if (link is null || link.OwnerId != request.UserId)
        {
            return null;
        }

        return new LinkDetailResult(
            link.Id,
            link.Slug,
            link.DestinationUrl,
            link.ClickCount,
            link.LastClickedAt,
            link.CreatedAt,
            link.UpdatedAt,
            link.ExpiresAt,
            link.IsDisabled);
    }

    public async Task<LinkAnalyticsResult?> GetLinkAnalyticsAsync(GetLinkAnalyticsRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(request));
        }

        if (request.LinkId == Guid.Empty)
        {
            throw new ArgumentException("Link id is required.", nameof(request));
        }

        var link = await _linkRepository.GetByIdAsync(request.LinkId, ct);
        if (link is null || link.OwnerId != request.UserId)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? today.AddDays(-(DefaultAnalyticsWindowDays - 1));
        var to = request.To ?? today;

        if (from > to)
        {
            throw new ArgumentException("Analytics start date must be before end date.", nameof(request));
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxAnalyticsWindowDays)
        {
            throw new ArgumentException($"Analytics range cannot be longer than {MaxAnalyticsWindowDays} days.", nameof(request));
        }

        var fromDateTime = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var daily = await _linkClickEventRepository.GetDailyAnalyticsAsync(link.Id, from, to, ct);
        var topCountries = await _linkClickEventRepository.GetTopCountriesAsync(
            link.Id,
            fromDateTime,
            toExclusive,
            TopCountriesLimit,
            ct);

        return new LinkAnalyticsResult(
            link.Id,
            link.ClickCount,
            link.LastClickedAt,
            from,
            to,
            daily,
            topCountries);
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

        var cachedLink = await _linkCacheRepository.GetBySlugAsync(request.Slug, ct);

        if (cachedLink != null)
        {
            await _clickEventQueue.EnqueueAsync(
                cachedLink.LinkId,
                DateTime.UtcNow,
                request.UserAgent,
                request.Referrer,
                request.IpHash,
                request.CountryCode,
                ct);

            return new ResolveLinkResult(cachedLink.DestinationUrl);
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

            var clickedAt = DateTime.UtcNow;

            await _linkCacheRepository.SetBySlugAsync(
                link.Slug,
                new LinkCacheEntry(link.Id, link.DestinationUrl),
                ct);

            await _clickEventQueue.EnqueueAsync(
                link.Id,
                clickedAt,
                request.UserAgent,
                request.Referrer,
                request.IpHash,
                request.CountryCode,
                ct);

            return new ResolveLinkResult(link.DestinationUrl);
        }

        return new ResolveLinkResult(null);
    }
}
