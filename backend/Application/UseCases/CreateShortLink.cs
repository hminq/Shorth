using System;
using Application.Abstractions;
using Application.Dtos;
using Domain.Entities;

namespace Application.UseCases;

public class CreateShortLink
{
    private readonly ILinkRepository _linkRepository;
    private readonly ISlugGenerator _slugGenerator;

    public CreateShortLink(ILinkRepository linkRepository, ISlugGenerator slugGenerator)
    {
        _linkRepository = linkRepository;
        _slugGenerator = slugGenerator;
    }

    public async Task<CreateLinkResult> ExecuteAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DestinationUrl))
        {
            throw new ArgumentException("Destination url is required.", nameof(request));
        }

        var createdAt = DateTime.UtcNow;
        var slug = await _slugGenerator.GenerateAsync(ct);

        var link = Link.Create(
            request.OwnerId,
            slug,
            request.DestinationUrl,
            createdAt,
            request.ExpiresAt
        );

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
}
