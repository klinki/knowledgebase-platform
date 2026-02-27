namespace SentinelKnowledgebase.Api.BackgroundProcessing;

public interface ICaptureProcessingQueue
{
    int QueueLength { get; }
    ValueTask QueueAsync(Guid rawCaptureId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken = default);
}
