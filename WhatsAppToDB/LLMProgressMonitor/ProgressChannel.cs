// Services/ProgressChannel.cs
using System.Threading.Channels;

public class ProgressChannel : IDisposable
{
    private readonly Channel<ProgressEvent> _channel =
        Channel.CreateUnbounded<ProgressEvent>(new UnboundedChannelOptions { SingleWriter = false });

    public ChannelWriter<ProgressEvent> Writer => _channel.Writer;
    public ChannelReader<ProgressEvent> Reader => _channel.Reader;

    public void Complete() => _channel.Writer.TryComplete();

    public void Dispose() => Complete();
}