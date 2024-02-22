using System;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Channel for synchronous and asynchronous requests.
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="M"></typeparam>
    public class RequestReplyChannel<R, M>: IRequestReplyChannel<R,M>
    {
        private readonly MessageHandlerList<IRequest<R, M>> _requestChannel = new MessageHandlerList<IRequest<R, M>>();

        /// <summary>
        /// Add a responder for requests.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IDisposable AddResponder(ISubscribableFiber fiber, Action<IRequest<R, M>> action)
        {
            var responder = new RequestReplyChannelSubscriber<R, M>(fiber, action);
            var disposable = _requestChannel.AddHandler(responder.OnReceive);
            responder.AddDisposable(disposable);
            return responder;
        }

        /// <summary>
        /// Send request to any and all subscribers.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>null if no subscribers registered for request.</returns>
        public IReply<M> SendRequest(R p)
        {
            var request = new ChannelRequest<R, M>(p);
            return _requestChannel.Publish(request) ? request : null;
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _requestChannel.Count; } }
    }
}
