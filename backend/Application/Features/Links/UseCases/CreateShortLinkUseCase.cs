using System;
using Application.Features.Links.Interfaces;
using Application.Features.Links.Dtos;
using Domain.Features.Links.Constants;
using Domain.Features.Links.Entities;
using Domain.Features.Links.Exceptions;

namespace Application.Features.Links.UseCases;

public class CreateShortLinkUseCase
{
    private readonly ILinkRepository _linkRepository;
    private readonly ISlugGenerator _slugGenerator;

    public CreateShortLinkUseCase(ILinkRepository linkRepository, ISlugGenerator slugGenerator)
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
        
        // Retry a few times before surfacing a failure to the caller.
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
}
