using System;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>, IDisposable
    {
        private readonly object _lock = new object();
        private readonly TRequestMessage _req;
        private readonly Action<TReplyMessage> _callbackOnReceive;
        private bool _disposed;

        public RequestReplyChannelRequest(TRequestMessage req, Action<TReplyMessage> callbackOnReceive)
        {
            _req = req;
            _callbackOnReceive = callbackOnReceive;
        }

        public TRequestMessage Request
        {
            get { return _req; }
        }

        public bool SendReply(TReplyMessage response)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return false;
                }
            }
            _callbackOnReceive?.Invoke(response);
            return true;
        }

        /// <summary>
        /// Stop receiving replies.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }
        }
    }
}
