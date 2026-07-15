using System.Threading.Channels;
using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;

namespace SnipLink.Infrastructure.Clicks;

public class ChannelClickRecorder : IClickRecorder
{
    private readonly Channel<ClickInfo> _channel;

    public ChannelClickRecorder(int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<ClickInfo>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelReader<ClickInfo> Reader => _channel.Reader;

    public bool Enqueue(ClickInfo click) => _channel.Writer.TryWrite(click);
}
