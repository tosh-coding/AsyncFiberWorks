using System;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscribes to last action received on the channel. 
    /// Subscription that delivers the latest message to the consuming thread.  If a newer message arrives before the consuming thread
    /// has a chance to process the message, the pending message is replaced by the newer message. The old message is discarded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LastSubscriber<T> : IDisposable
    {
        private readonly object _batchLock = new object();
        private readonly long _intervalInMs;
        private readonly IExecutionContext _batchFiber;
        private readonly IExecutionContext _executeFiber;
        private readonly Action<T> _receive;

        private bool _flushPending;
        private T _pending;
        private IDisposable _timerAction;

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastSubscriber(long intervalInMs, Action<T> receive)
            : this(intervalInMs, null, receive)
        {
        }

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastSubscriber(long intervalInMs, IExecutionContext fiber, Action<T> receive)
            : this(intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastSubscriber(long intervalInMs, IExecutionContext batchFiber, IExecutionContext fiber, Action<T> receive)
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
        /// Message receiving function.
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveOnProducerThread(T msg)
        {
            lock (_batchLock)
            {
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