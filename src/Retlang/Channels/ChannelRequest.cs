using System;
using System.Collections.Generic;
using System.Threading;
using Retlang.Core;

namespace Retlang.Channels
{
    internal class ChannelRequest<R, M> : IRequest<R, M>, IReply<M>
    {
        private readonly object _lock = new object();
        private readonly R _req;
        private readonly Queue<M> _resp = new Queue<M>();
        private bool _disposed;
        private IExecutionContext _fiberOnReceive = null;
        private Action<object> _callbackOnReceive = null;
        private Timer _timer = null;
        private object _timerId = null;
        private object _argumentOfCallback = null;

        public ChannelRequest(R req)
        {
            _req = req;
        }

        public R Request
        {
            get { return _req; }
        }

        public bool SendReply(M response)
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

        public bool TryReceive(out M result)
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
                    result = default(M);
                    return false;
                }
                result = default(M);
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
                    ClearCallbackOnReceive();
                }

                if (_resp.Count < 0)
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
