using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>, IReply<TReplyMessage>
    {
        private readonly object _lock = new object();
        private readonly TRequestMessage _req;
        private readonly Queue<TReplyMessage> _resp = new Queue<TReplyMessage>();
        private bool _disposed;
        private Action _callbackOnReceive = null;

        public RequestReplyChannelRequest(TRequestMessage req)
        {
            _req = req;
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
                _resp.Enqueue(response);

                var callbackOnReceive = _callbackOnReceive;
                callbackOnReceive?.Invoke();
                return true;
            }
        }

        public bool TryReceive(out TReplyMessage result)
        {
            lock (_lock)
            {
                if (_resp.Count > 0)
                {
                    result = _resp.Dequeue();
                    return true;
                }
                result = default(TReplyMessage);
                return false;
            }
        }

        public bool SetCallbackOnReceive(Action callbackOnReceive)
        {
            if (callbackOnReceive == null)
            {
                throw new ArgumentNullException(nameof(callbackOnReceive));
            }
            lock (_lock)
            {
                if (_disposed)
                {
                    return false;
                }

                _callbackOnReceive = callbackOnReceive;

                if (_resp.Count > 0)
                {
                    callbackOnReceive();
                }
            }
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
                _callbackOnReceive = null;
            }
        }
    }
}
