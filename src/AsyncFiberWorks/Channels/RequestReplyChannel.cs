using System;
using AsyncFiberWorks.Core;

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
        /// <param name="action">A responder.</param>
        /// <returns>Handler for cancellation.</returns>
        public IDisposable AddResponder(Action<IRequest<R, M>> action)
        {
            return _requestChannel.AddHandler(action);
        }

        /// <summary>
        /// Send request to any and all subscribers.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>null if no subscribers registered for request.</returns>
        public IReply<M> SendRequest(R p)
        {
            var request = new RequestReplyChannelRequest<R, M>(p);
            return _requestChannel.Publish(request) ? request : null;
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _requestChannel.Count; } }
    }
}
