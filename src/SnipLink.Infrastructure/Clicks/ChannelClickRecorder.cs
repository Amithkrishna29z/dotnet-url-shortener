using System.Threading.Channels;
using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;

namespace SnipLink.Infrastructure.Clicks;

/// <summary>
/// Buffers clicks in an in-memory bounded channel so recording never blocks the
/// redirect. <see cref="ClickFlushService"/> drains the channel and persists clicks.
/// If the buffer is full (sustained spike), new clicks are dropped rather than
/// slowing redirects — analytics is best-effort, redirect latency is not.
/// </summary>
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
