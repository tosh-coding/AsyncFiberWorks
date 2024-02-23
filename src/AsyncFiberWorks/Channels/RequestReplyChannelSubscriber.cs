using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelSubscriber<R, M> : IDisposableSubscriptionRegistry, IDisposable
    {
        private readonly IExecutionContext _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        public RequestReplyChannelSubscriber(IExecutionContext fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
        }

        public void AddDisposable(IDisposable disposable)
        {
            _unsubscriber.Add(() => disposable.Dispose());
        }

        /// <summary>
        /// <see cref="IDisposableSubscriptionRegistry.BeginSubscription"/>
        /// </summary>
        public Unsubscriber BeginSubscription()
        {
            return _unsubscriber.BeginSubscription();
        }

        public void OnReceive(IRequest<R, M> msg)
        {
            _fiber.Enqueue(() => _onRequest(msg));
        }
    }
}
