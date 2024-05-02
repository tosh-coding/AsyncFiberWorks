using System;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// If a new one arrives, the newer one has priority. The older one will disappear.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LastFilter<T> : IDisposable
    {
        private readonly object _batchLock = new object();
        private readonly long _intervalInMs;
        private readonly IExecutionContext _batchFiber;
        private readonly IExecutionContext _executeFiber;
        private readonly Action<T> _receive;

        private bool _flushPending;
        private T _pending;
        private IDisposable _timerAction;
        private bool _disposed;

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastFilter(long intervalInMs, Action<T> receive)
            : this(intervalInMs, new PoolFiberSlim(), receive)
        {
        }

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastFilter(long intervalInMs, IExecutionContext fiber, Action<T> receive)
            : this(intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastFilter(long intervalInMs, IExecutionContext batchFiber, IExecutionContext fiber, Action<T> receive)
        {
            _intervalInMs = intervalInMs;
            _batchFiber = batchFiber ?? ((IExecutionContext)fiber);
            _executeFiber = fiber ?? throw new ArgumentNullException(nameof(fiber));
            _receive = receive;
        }

        /// <summary>
        /// Unsubscribe the fiber, discards added disposables, and cancel the batching timer
        /// </summary>
        public void Dispose()
        {
            lock (_batchLock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                if (_timerAction != null)
                {
                    _timerAction.Dispose();
                    _timerAction = null;
                }
            }
        }

        /// <summary>
        /// Message receiving function.
        /// </summary>
        /// <param name="msg"></param>
        public void Receive(T msg)
        {
            lock (_batchLock)
            {
                if (_disposed)
                {
                    return;
                }
                if (!_flushPending)
                {
                    _timerAction = OneshotTimerAction.StartNew(() => _batchFiber.Enqueue(Flush), _intervalInMs);
                    _flushPending = true;
                }
                _pending = msg;
            }
        }

        private void Flush()
        {
            var toFlush = ClearPending();
            if ((_executeFiber != null) && (_batchFiber != _executeFiber))
            {
                _executeFiber.Enqueue(() => _receive(toFlush));
            }
            else
            {
                _receive(toFlush);
            }
        }

        private T ClearPending()
        {
            lock (_batchLock)
            {
                _flushPending = false;
                return _pending;
            }
        }
    }
}