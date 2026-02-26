using System.Threading.Channels;

namespace SentinelKnowledgebase.Api.BackgroundProcessing;

public class CaptureProcessingQueue : ICaptureProcessingQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public ValueTask QueueAsync(Guid rawCaptureId, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(rawCaptureId, cancellationToken);
    }

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}
