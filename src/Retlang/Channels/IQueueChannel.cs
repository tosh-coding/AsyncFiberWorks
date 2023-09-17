using System;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Creates a queue that will deliver a message to a single consumer. Load balancing can be achieved by creating 
    /// multiple subscribers to the queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueueChannel<T> : ISubscriberQueueChannel<T>, IPublisherQueueChannel<T>
    {
    }
}
