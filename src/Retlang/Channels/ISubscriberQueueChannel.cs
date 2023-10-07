using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// The interface for QueueChannel subscribers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriberQueueChannel<T>
    {
        /// <summary>
        /// Subscribe to the context.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onMessage"></param>
        /// <returns></returns>
        IDisposable Subscribe(IFiberWithFallbackRegistry fiber, Action<T> onMessage);

        /// <summary>
        /// Subscribe to the context.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onMessage"></param>
        /// <param name="fallbackRegistry"></param>
        /// <returns></returns>
        IDisposable Subscribe(IExecutionContext fiber, Action<T> onMessage, ISubscriptionRegistry fallbackRegistry);
    }
}
