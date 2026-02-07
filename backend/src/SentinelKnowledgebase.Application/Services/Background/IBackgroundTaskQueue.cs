namespace SentinelKnowledgebase.Application.Services.Background;

/// <summary>
/// Interface for a background task queue that safely processes tasks with proper error handling
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queue a background task for processing
    /// </summary>
    /// <param name="workItem">The function to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when the item is queued</returns>
    ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue a background task for processing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The dequeued work item</returns>
    ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
