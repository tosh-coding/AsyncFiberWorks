namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// A request object.
    /// </summary>
    /// <typeparam name="TRequestMessage"></typeparam>
    /// <typeparam name="TReplyMessage"></typeparam>
    public class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>
    {
        private readonly TRequestMessage _req;
        private readonly IPublisher<TReplyMessage> _replyTo;

        /// <summary>
        /// Create a request object.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="replyTo"></param>
        public RequestReplyChannelRequest(TRequestMessage req, IPublisher<TReplyMessage> replyTo)
        {
            _req = req;
            _replyTo = replyTo;
        }

        /// <summary>
        /// Request Message
        /// </summary>
        public TRequestMessage Request
        {
            get { return _req; }
        }

        /// <summary>
        /// Message reply to.
        /// </summary>
        public IPublisher<TReplyMessage> ReplyTo
        {
            get { return _replyTo; }
        }
    }
}
