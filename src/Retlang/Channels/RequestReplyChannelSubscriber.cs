using Retlang.Fibers;
using System;

namespace Retlang.Channels
{
    public class RequestReplyChannelSubscriber<R, M>
    {
        private readonly IFiberWithFallbackRegistry _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;

        public RequestReplyChannelSubscriber(IFiberWithFallbackRegistry fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
        }

        public IDisposable Subscribe(RequestReplyChannel<R, M> channel)
        {
            var disposable = channel.OnSubscribe(OnReceive);
            return _fiber.FallbackDisposer?.RegisterSubscriptionAndCreateDisposable(disposable) ?? disposable;
        }

        public void OnReceive(IRequest<R, M> msg)
        {
            _fiber.Enqueue(() => _onRequest(msg));
        }
    }
}
