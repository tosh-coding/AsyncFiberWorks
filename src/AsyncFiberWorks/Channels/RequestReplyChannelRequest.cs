using System;
using System.Collections.Generic;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    internal class RequestReplyChannelRequest<TRequestMessage, TReplyMessage> : IRequest<TRequestMessage, TReplyMessage>, IReply<TReplyMessage>
    {
        private readonly object _lock = new object();
        private readonly TRequestMessage _req;
        private readonly Queue<TReplyMessage> _resp = new Queue<TReplyMessage>();
        private bool _disposed;
        private IExecutionContext _fiberOnReceive = null;
        private Action<object> _callbackOnReceive = null;
        private Timer _timer = null;
        private object _timerId = null;
        private object _argumentOfCallback = null;

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
                    var fiberOnReceive = _fiberOnReceive;
                    var callbackOnReceive = _callbackOnReceive;
                    var argumentOfCallback = _argumentOfCallback;
                    ClearCallbackOnReceive();
                    EnqueueOnReceive(fiberOnReceive, callbackOnReceive, argumentOfCallback);
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

        public bool SetCallbackOnReceive(int timeoutInMs, IExecutionContext fiberOnReceive, Action<object> callbackOnReceive, object argumentOfCallback = null)
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
                    var oldFiberOnReceive = _fiberOnReceive;
                    var oldCallbackOnReceive = _callbackOnReceive;
                    var oldArgumentOfCallback = _argumentOfCallback;
                    ClearCallbackOnReceive();
                    EnqueueOnReceive(oldFiberOnReceive, oldCallbackOnReceive, oldArgumentOfCallback);
                }

                if (_resp.Count <= 0)
                {
                    _fiberOnReceive = fiberOnReceive;
                    _callbackOnReceive = callbackOnReceive;
                    _argumentOfCallback = argumentOfCallback;
                    _timerId = new object();
                    _timer = new Timer(OnTimeout, _timerId, timeoutInMs, 0);
                }
                else
                {
                    EnqueueOnReceive(fiberOnReceive, callbackOnReceive, argumentOfCallback);
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
                _fiberOnReceive = null;
                _callbackOnReceive = null;
                _argumentOfCallback = null;
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

                var fiberOnReceive = _fiberOnReceive;
                var callbackOnReceive = _callbackOnReceive;
                var argumentOfCallback = _argumentOfCallback;
                ClearCallbackOnReceive();
                EnqueueOnReceive(fiberOnReceive, callbackOnReceive, argumentOfCallback);
            }
        }

        private void EnqueueOnReceive(IExecutionContext fiberOnReceive, Action<object> callbackOnReceive, object argumentOfCallback)
        {
            if (fiberOnReceive != null)
            {
                fiberOnReceive.Enqueue(() =>
                {
                    callbackOnReceive(argumentOfCallback);
                });
            }
            else
            {
                DefaultThreadPool.Instance.Queue((_) =>
                {
                    callbackOnReceive(argumentOfCallback);
                });
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
                    _fiberOnReceive = null;
                    _callbackOnReceive = null;
                    _argumentOfCallback = null;
                }
            }
        }
    }
}
