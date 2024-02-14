using Retlang.Fibers;
using System;

namespace Retlang.Channels
{
    public class RequestReplyChannelSubscriber<R, M> : IDisposable
    {
        private readonly IFiberWithFallbackRegistry _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;
        private IDisposable _disposable;

        public RequestReplyChannelSubscriber(IFiberWithFallbackRegistry fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
        }

        public void Subscribe(RequestReplyChannel<R, M> channel)
        {
            var disposable = channel.OnSubscribe(OnReceive);
            var unsubscriber = _fiber.FallbackDisposer?.CreateSubscription();
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
