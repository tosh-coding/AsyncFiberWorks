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
        IDisposable Subscribe(IFiber fiber, Action<T> onMessage);

        /// <summary>
        /// Subscribe to the context.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onMessage"></param>
        /// <param name="fallbackRegistry"></param>
        /// <returns></returns>
        IDisposable Subscribe(IExecutionContext fiber, Action<T> onMessage, ISubscriptionRegistry fallbackRegistry);

        /// <summary>
        /// Persistent subscribe to the context. This subscription cannot be unsubscribed. 
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="onMessage"></param>
        void PersistentSubscribe(IExecutionContext executionContext, Action<T> onMessage);
    }
}
