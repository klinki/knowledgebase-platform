using System.Threading.Channels;
using System.Runtime.CompilerServices;

namespace SentinelKnowledgebase.Api.BackgroundProcessing;

public class CaptureProcessingQueue : ICaptureProcessingQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    private int _queueLength;

    public int QueueLength => Volatile.Read(ref _queueLength);

    public async ValueTask QueueAsync(Guid rawCaptureId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _queueLength);

        try
        {
            await _queue.Writer.WriteAsync(rawCaptureId, cancellationToken);
        }
        catch
        {
            Interlocked.Decrement(ref _queueLength);
            throw;
        }
    }

    public async IAsyncEnumerable<Guid> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var rawCaptureId in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            Interlocked.Decrement(ref _queueLength);
            yield return rawCaptureId;
        }
    }
}
