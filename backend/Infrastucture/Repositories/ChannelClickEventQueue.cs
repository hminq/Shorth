using Application.Features.Links.Interfaces;
using Application.Features.Links.Messages;
using Microsoft.Extensions.Logging;

namespace Infrastucture.Repositories;

public sealed class ChannelClickEventQueue : IClickEventQueue
{
    private readonly ClickEventChannel _channel;
    private readonly ILogger<ChannelClickEventQueue> _logger;

    public ChannelClickEventQueue(
        ClickEventChannel channel,
        ILogger<ChannelClickEventQueue> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public Task EnqueueAsync(
        Guid linkId,
        DateTime clickedAt,
        string? userAgent,
        string? referrer,
        string? ipHash,
        string? countryCode,
        CancellationToken ct = default)
    {
        var message = new ClickEventMessage(
            Guid.NewGuid(),
            linkId,
            clickedAt,
            userAgent,
            referrer,
            ipHash,
            countryCode);

        if (!_channel.Writer.TryWrite(message))
        {
            _logger.LogWarning(
                "Click event channel is full. Dropping click event for link {LinkId}. Capacity: {Capacity}",
                linkId,
                ClickEventChannel.Capacity);
        }

        return Task.CompletedTask;
    }
}
