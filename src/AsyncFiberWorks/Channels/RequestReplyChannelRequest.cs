using System;
using System.Collections.Generic;
using System.Threading;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>, IReply<TReplyMessage>
    {
        private readonly object _lock = new object();
        private readonly TRequestMessage _req;
        private readonly Queue<TReplyMessage> _resp = new Queue<TReplyMessage>();
        private bool _disposed;
        private Action _callbackOnReceive = null;
        private Timer _timer = null;
        private object _timerId = null;

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

                if (_callbackOnReceive != null)
                {
                    var callbackOnReceive = _callbackOnReceive;
                    ClearCallbackOnReceive();
                    callbackOnReceive();
                }
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
                if (_disposed)
                {
                    result = default(TReplyMessage);
                    return false;
                }
                result = default(TReplyMessage);
                return false;
            }
        }

        public bool SetCallbackOnReceive(int timeoutInMs, Action callbackOnReceive)
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
                if (_timer != null)
                {
                    var oldCallbackOnReceive = _callbackOnReceive;
                    ClearCallbackOnReceive();
                    oldCallbackOnReceive();
                }

                if (_resp.Count <= 0)
                {
                    _callbackOnReceive = callbackOnReceive;
                    _timerId = new object();
                    _timer = new Timer(OnTimeout, _timerId, timeoutInMs, 0);
                }
                else
                {
                    callbackOnReceive();
                }
            }
            return true;
        }

        public void ClearCallbackOnReceive()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                if (_timer == null)
                {
                    return;
                }
                _timer.Dispose();
                _timer = null;
                _timerId = null;
                _callbackOnReceive = null;
            }
        }

        private void OnTimeout(object timerId)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                if (_timer == null)
                {
                    return;
                }
                if (_timerId != timerId)
                {
                    return;
                }

                var callbackOnReceive = _callbackOnReceive;
                ClearCallbackOnReceive();
                callbackOnReceive();
            }
        }

        /// <summary>
        /// Stop receiving replies.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                    _timerId = null;
                    _callbackOnReceive = null;
                }
            }
        }
    }
}
