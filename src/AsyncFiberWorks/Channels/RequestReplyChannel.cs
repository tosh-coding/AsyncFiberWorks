using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Channel for synchronous and asynchronous requests.
    /// </summary>
    /// <typeparam name="TRequestMessage"></typeparam>
    /// <typeparam name="TReplyMessage"></typeparam>
    public class RequestReplyChannel<TRequestMessage, TReplyMessage>: IRequestReplyChannel<TRequestMessage,TReplyMessage>
    {
        private readonly MessageHandlerList<IRequest<TRequestMessage, TReplyMessage>> _requestChannel = new MessageHandlerList<IRequest<TRequestMessage, TReplyMessage>>();

        /// <summary>
        /// Add a responder for requests.
        /// </summary>
        /// <param name="action">A responder.</param>
        /// <returns>Handler for cancellation.</returns>
        public IDisposable AddResponder(Action<IRequest<TRequestMessage, TReplyMessage>> action)
        {
            return _requestChannel.AddHandler(action);
        }

        /// <summary>
        /// Send request to any and all subscribers.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="callbackOnReceive">Message receive handler.</param>
        /// <returns>Handler for cancellation.</returns>
        public IDisposable SendRequest(TRequestMessage p, Action<TReplyMessage> callbackOnReceive)
        {
            var request = new RequestReplyChannelRequest<TRequestMessage, TReplyMessage>(p, callbackOnReceive);
            _requestChannel.Publish(request);
            return request;
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _requestChannel.Count; } }
    }
}
