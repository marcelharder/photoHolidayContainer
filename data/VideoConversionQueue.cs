
using System.Threading.Channels;

namespace photoHolidayContainer.data;

public sealed class VideoConversionQueue
{
    private readonly Channel<string> _channel =
        Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

    public ValueTask QueueAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(
            videoPath,
            cancellationToken);
    }

    public IAsyncEnumerable<string> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(
            cancellationToken);
    }
}
