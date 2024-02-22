using AsyncFiberWorks.Fibers;
using System;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelSubscriber<R, M> : IDisposable
    {
        private readonly ISubscribableFiber _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        public RequestReplyChannelSubscriber(ISubscribableFiber fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
            fiber.BeginSubscriptionAndSetUnsubscriber(_unsubscriber);
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
        }

        public void AddDisposable(IDisposable disposable)
        {
            _unsubscriber.Add(() => disposable.Dispose());
        }

        public void OnReceive(IRequest<R, M> msg)
        {
            _fiber.Enqueue(() => _onRequest(msg));
        }
    }
}
