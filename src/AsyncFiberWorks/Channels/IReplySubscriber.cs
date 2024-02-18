using System;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Methods for working with a replyChannel
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="M"></typeparam>
    public interface IReplySubscriber<R, M>
    {
        /// <summary>
        /// Subscribe to a request on the channel.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IDisposable OnSubscribe(Action<IRequest<R, M>> action);
    }
}
