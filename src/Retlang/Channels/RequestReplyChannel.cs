using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Channel for synchronous and asynchronous requests.
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="M"></typeparam>
    public class RequestReplyChannel<R, M>: IRequestReplyChannel<R,M>
    {
        private readonly InternalChannel<IRequest<R, M>> _requestChannel = new InternalChannel<IRequest<R, M>>();

        /// <summary>
        /// Subscribe to requests.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onRequest"></param>
        /// <param name="registry"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IExecutionContext fiber, Action<IRequest<R, M>> onRequest, ISubscriptionRegistry registry)
        {
            Action<IRequest<R, M>> action = (msg) =>
            {
                fiber.Enqueue(() => onRequest(msg));
            };
            var disposable = _requestChannel.SubscribeOnProducerThreads(action);
            return registry?.RegisterSubscriptionAndCreateDisposable(disposable) ?? disposable;
        }

        /// <summary>
        /// Subscribe to requests.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onRequest"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IFiberWithFallbackRegistry fiber, Action<IRequest<R, M>> onRequest)
        {
            return Subscribe(fiber, onRequest, fiber.FallbackDisposer);
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
        public int NumSubscribers { get { return _requestChannel.NumSubscribers; } }
    }
}
