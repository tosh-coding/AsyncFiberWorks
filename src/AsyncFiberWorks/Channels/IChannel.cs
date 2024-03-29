using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// A channel provides a conduit for messages. It provides methods for publishing and subscribing to messages. 
    /// The class is thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IChannel<T> : ISubscriber<T>, IPublisher<T>
    {
    }
}
