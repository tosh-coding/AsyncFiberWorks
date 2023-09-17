using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
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
        /// <param name="fiber"></param>
        /// <param name="onRequest"></param>
        /// <returns></returns>
        IDisposable Subscribe(IFiber fiber, Action<IRequest<R, M>> onRequest);

        /// <summary>
        /// Persistent subscribe to requests. This subscription cannot be unsubscribed. 
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="onRequest"></param>
        void PersistentSubscribe(IExecutionContext executionContext, Action<IRequest<R, M>> onRequest);
    }
}
