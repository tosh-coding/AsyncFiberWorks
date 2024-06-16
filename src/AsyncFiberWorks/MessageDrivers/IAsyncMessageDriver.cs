using System;

namespace AsyncFiberWorks.MessageDrivers
{
    /// <summary>
    /// A message driver provides methods for message distribution and subscription.
    /// Non-thread-safe, assuming single-threaded processing.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IAsyncMessageDriver<TMessage> : IAsyncMessageDriverSubscriber<TMessage>, IAsyncMessageDriverDistributor<TMessage>
    {
    }
}
