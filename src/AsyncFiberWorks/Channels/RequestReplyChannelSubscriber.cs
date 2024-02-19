using AsyncFiberWorks.Fibers;
using System;

namespace AsyncFiberWorks.Channels
{
    public class RequestReplyChannelSubscriber<R, M> : IDisposable
    {
        private readonly ISubscribableFiber _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;
        private IDisposable _disposable;

        public RequestReplyChannelSubscriber(ISubscribableFiber fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
        }

        public void Subscribe(RequestReplyChannel<R, M> channel)
        {
            var disposable = channel.OnSubscribe(OnReceive);
            var unsubscriber = _fiber.CreateSubscription();
            if (unsubscriber != null)
            {
                unsubscriber.Add(() => disposable.Dispose());
            }
            _disposable = unsubscriber ?? disposable;
        }

        public void Dispose()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
        }

        public void OnReceive(IRequest<R, M> msg)
        {
            _fiber.Enqueue(() => _onRequest(msg));
        }
    }
}
