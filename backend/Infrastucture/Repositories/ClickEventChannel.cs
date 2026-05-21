using Application.Features.Links.Messages;
using System.Threading.Channels;

namespace Infrastucture.Repositories;

public sealed class ClickEventChannel
{
    public const int Capacity = 10_000;

    private readonly Channel<ClickEventMessage> _channel = Channel.CreateBounded<ClickEventMessage>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public ChannelReader<ClickEventMessage> Reader => _channel.Reader;
    public ChannelWriter<ClickEventMessage> Writer => _channel.Writer;
}
