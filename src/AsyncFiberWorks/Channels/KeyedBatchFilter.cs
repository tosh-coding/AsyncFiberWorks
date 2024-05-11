using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Arriving events are buffered once and sent to the next recipient a few moments later.
    /// If there are duplicate keys, the newer one takes precedence. The old one disappears.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="T"></typeparam>
    public class KeyedBatchFilter<K, T> : IDisposable
    {
        private readonly object _batchLock = new object();

        private readonly Converter<T, K> _keyResolver;
        private readonly long _intervalInMs;
        private readonly IExecutionContext _batchFiber;
        private readonly IExecutionContext _executeFiber;
        private readonly Action<IDictionary<K, T>> _receive;

        private Dictionary<K, T> _pending;
        private IDisposable _timerAction;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver">The process of retrieving a key from a message.</param>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchFilter(Converter<T, K> keyResolver, long intervalInMs, Action<IDictionary<K, T>> receive)
            : this(keyResolver, intervalInMs, new PoolFiber(), receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver">The process of retrieving a key from a message.</param>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchFilter(Converter<T, K> keyResolver, long intervalInMs, IExecutionContext fiber, Action<IDictionary<K, T>> receive)
            : this(keyResolver, intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver">The process of retrieving a key from a message.</param>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchFilter(Converter<T, K> keyResolver, long intervalInMs, IExecutionContext batchFiber, IExecutionContext fiber, Action<IDictionary<K, T>> receive)
        {
            _keyResolver = keyResolver;
            _intervalInMs = intervalInMs;
            _batchFiber = batchFiber ?? ((IExecutionContext)fiber);
            _executeFiber = fiber ?? throw new ArgumentNullException(nameof(fiber));
            _receive = receive;
        }

        /// <summary>
        /// Cancel the batching timer.
        /// </summary>
        public void Dispose()
        {
            lock (_batchLock)
            {
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
                var key = _keyResolver(msg);
                if (_pending == null)
                {
                    _pending = new Dictionary<K, T>();
                    _timerAction = OneshotTimerAction.StartNew(() => _batchFiber.Enqueue(Flush), _intervalInMs);
                }
                _pending[key] = msg;
            }
        }

        private void Flush()
        {
            var toReturn = ClearPending();
            if (toReturn != null)
            {
                _executeFiber.Enqueue(() => _receive(toReturn));
            }
        }

        private IDictionary<K, T> ClearPending()
        {
            lock (_batchLock)
            {
                if (_pending == null || _pending.Count == 0)
                {
                    _pending = null;
                    return null;
                }
                IDictionary<K, T> toReturn = _pending;
                _pending = null;
                return toReturn;
            }
        }
    }
}