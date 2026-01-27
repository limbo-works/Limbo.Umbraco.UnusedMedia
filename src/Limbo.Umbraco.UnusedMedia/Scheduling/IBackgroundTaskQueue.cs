using System;
using System.Threading;
using System.Threading.Tasks;

namespace Limbo.Umbraco.UnusedMedia.Scheduling;

/// <summary>
/// Interface for a background task queue.
/// </summary>
public interface IBackgroundTaskQueue {

    /// <summary>
    /// Queues a background work item.
    /// </summary>
    /// <param name="workItem">The work item to queue.</param>
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

    /// <summary>
    /// Dequeues a background work item.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The dequeued work item.</returns>
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);

}
