using AsyncFiberWorks.Fibers;
using System;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelSubscriber<R, M> : IDisposable
    {
        private readonly ISubscribableFiber _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;
        private IDisposable _disposable;

        public RequestReplyChannelSubscriber(ISubscribableFiber fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
        }

        public void Dispose()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
        }

        public void SetDisposable(IDisposable disposable)
        {
            _disposable = disposable;
        }

        public void OnReceive(IRequest<R, M> msg)
        {
            _fiber.Enqueue(() => _onRequest(msg));
        }
    }
}
