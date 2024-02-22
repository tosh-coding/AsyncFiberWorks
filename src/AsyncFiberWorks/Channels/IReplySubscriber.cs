using System;
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
        /// Add a responder for requests.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IDisposable AddResponder(ISubscribableFiber fiber, Action<IRequest<R, M>> action);
    }
}