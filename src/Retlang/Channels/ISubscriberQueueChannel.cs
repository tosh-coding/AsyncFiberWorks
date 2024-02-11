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
        /// <param name="action"></param>
        /// <param name="outQueue"></param>
        /// <returns></returns>
        IDisposable OnSubscribe(Action<byte> action, out IMessageQueue<T> outQueue);
    }
}
