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
            Action action;
            lock (_lock)
            {
                if (_disposed)
                {
                    return false;
                }
                _resp.Enqueue(response);

                action = _callbackOnReceive;
                _callbackOnReceive = null;
            }
            action?.Invoke();
            return true;
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
            bool hasResponse = false;
            lock (_lock)
            {
                if (_disposed)
                {
                    return false;
                }

                hasResponse = _resp.Count > 0;
                if (hasResponse)
                {
                    _callbackOnReceive = null;
                }
                else
                {
                    _callbackOnReceive = callbackOnReceive;
                }
            }
            if (hasResponse)
            {
                callbackOnReceive?.Invoke();
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
