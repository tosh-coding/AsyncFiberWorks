namespace AsyncFiberWorks.Channels
{
    public class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>
    {
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
