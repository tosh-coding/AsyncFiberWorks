using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Timers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Arriving events are buffered once and sent to the next recipient a few moments later.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BatchFilter<T> : IDisposable
    {
        private readonly object _batchLock = new object();
        
        private readonly long _intervalInMs;
        private readonly IExecutionContext _batchFiber;
        private readonly IExecutionContext _executeFiber;
        private readonly Action<IList<T>> _receive;

        private List<T> _pending;
        private IDisposable _timerAction;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="receive">Message receiving handler.</param>
        public BatchFilter(long intervalInMs, Action<IList<T>> receive)
            : this(intervalInMs, new PoolFiber(), receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public BatchFilter(long intervalInMs, IExecutionContext fiber, Action<IList<T>> receive)
            : this(intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="intervalInMs">Batch processing interval. Milliseconds.</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public BatchFilter(long intervalInMs, IExecutionContext batchFiber, IExecutionContext fiber, Action<IList<T>> receive)
        {
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
        /// Receives message and batches as needed.
        /// </summary>
        /// <param name="msg"></param>
        public void Receive(T msg)
        {
            lock (_batchLock)
            {
                if (_pending == null)
                {
                    _pending = new List<T>();
                    _timerAction = OneshotTimerAction.StartNew(() => _batchFiber.Enqueue(Flush), _intervalInMs);
                }
                _pending.Add(msg);
            }
        }

        private void Flush()
        {
            IList<T> toFlush = null;
            lock (_batchLock)
            {
                if (_pending != null)
                {
                    toFlush = _pending;
                    _pending = null;
                }
            }
            if (toFlush != null)
            {
                _executeFiber.Enqueue(() => _receive(toFlush));
            }
        }
    }
}