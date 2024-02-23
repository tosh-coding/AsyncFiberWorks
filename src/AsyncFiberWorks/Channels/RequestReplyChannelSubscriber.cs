using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelSubscriber<R, M>
    {
        private readonly IExecutionContext _fiber;
        private readonly Action<IRequest<R, M>> _onRequest;

        public RequestReplyChannelSubscriber(IExecutionContext fiber, Action<IRequest<R, M>> onRequest)
        {
            _fiber = fiber;
            _onRequest = onRequest;
        }

        public void OnReceive(IRequest<R, M> msg)
        {
            _fiber.Enqueue(() => _onRequest(msg));
        }
    }
}
