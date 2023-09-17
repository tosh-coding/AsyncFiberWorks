using System;
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
    }
}
