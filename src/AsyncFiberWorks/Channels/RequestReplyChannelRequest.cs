using System;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>
    {
        private readonly object _lock = new object();
        private readonly TRequestMessage _req;
        private readonly IPublisher<TReplyMessage> _replyTo;

        public RequestReplyChannelRequest(TRequestMessage req, IPublisher<TReplyMessage> replyTo)
        {
            _req = req;
            _replyTo = replyTo;
        }

        public TRequestMessage Request
        {
            get { return _req; }
        }

        public IPublisher<TReplyMessage> ReplyTo
        {
            get { return _replyTo; }
        }
    }
}
