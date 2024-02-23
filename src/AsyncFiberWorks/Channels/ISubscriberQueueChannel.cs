using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// The interface for QueueChannel subscription.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriberQueueChannel<T>
    {
        /// <summary>
        /// Subscribe to executor messages. 
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IDisposableSubscriptionRegistry Subscribe(IExecutionContext fiber, Action<T> callback);
    }
}
