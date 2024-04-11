using Azure.Storage.Queues;

namespace EntraMfaPrefillinator.Lib.Azure.Services;

/// <summary>
/// Interface for the queue client service.
/// </summary>
public interface IQueueClientService
{
    /// <summary>
    /// Queue client for the AuthUpdate queue.
    /// </summary>
    QueueClient AuthUpdateQueueClient { get; }
}