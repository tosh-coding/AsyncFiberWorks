using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Batches actions for the consuming thread.
    /// Subscribes to actions on the channel in batch form. The events will be batched if the consumer is unable to process the events 
    /// faster than the arrival rate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BatchSubscriber<T> : IDisposable
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
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="receive">Message receiving handler.</param>
        public BatchSubscriber(long intervalInMs, Action<IList<T>> receive)
            : this(intervalInMs, null, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public BatchSubscriber(long intervalInMs, IExecutionContext fiber, Action<IList<T>> receive)
            : this(intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public BatchSubscriber(long intervalInMs, IExecutionContext batchFiber, IExecutionContext fiber, Action<IList<T>> receive)
        {
            _intervalInMs = intervalInMs;
            _batchFiber = batchFiber ?? ((IExecutionContext)fiber) ?? new PoolFiberSlim();
            _executeFiber = fiber;
            _receive = receive;
        }

        /// <summary>
        /// Unsubscribe the fiber, discards added disposables, and cancel the batching timer
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
        public void ReceiveOnProducerThread(T msg)
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
                if ((_executeFiber != null) && (_batchFiber != _executeFiber))
                {
                    _executeFiber.Enqueue(() => _receive(toFlush));
                }
                else
                {
                    _receive(toFlush);
                }
            }
        }
    }
}