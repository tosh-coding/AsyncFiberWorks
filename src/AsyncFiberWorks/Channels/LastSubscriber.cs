using System;
using System.Threading;
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
    public class LastSubscriber<T> : IMessageReceiver<T>, IDisposable
    {
        private readonly object _batchLock = new object();
        private readonly IMessageFilter<T> _filter;
        private readonly long _intervalInMs;
        private readonly IExecutionContext _batchFiber;
        private readonly ISubscribableFiber _executeFiber;
        private readonly Action<T> _receive;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        private bool _flushPending;
        private T _pending;

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
        /// <param name="fiber"></param>
        /// <param name="receive">Message receiving handler.</param>
        public LastSubscriber(long intervalInMs, ISubscribableFiber fiber, Action<T> receive)
            : this(null, intervalInMs, fiber, receive)
        {
        }

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastSubscriber(IMessageFilter<T> filter, long intervalInMs, ISubscribableFiber fiber, Action<T> receive)
            : this(filter, intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive">Message receiving handler.</param>
        public LastSubscriber(IMessageFilter<T> filter, long intervalInMs, IExecutionContext batchFiber, ISubscribableFiber fiber, Action<T> receive)
        {
            _filter = filter;
            _intervalInMs = intervalInMs;
            _batchFiber = batchFiber ?? ((IExecutionContext)fiber) ?? new PoolFiberSlim();
            _executeFiber = fiber;
            _receive = receive;
            fiber.BeginSubscriptionAndSetUnsubscriber(_unsubscriber);
        }

        /// <summary>
        /// <see cref="IMessageReceiver{T}.AddDisposable(IDisposable)"/>
        /// </summary>
        /// <param name="disposable"></param>
        public void AddDisposable(IDisposable disposable)
        {
            _unsubscriber.Add(() => disposable.Dispose());
        }

        /// <summary>
        /// Unsubscribe the fiber, discards added disposables, and cancel the batching timer
        /// </summary>
        public void Dispose()
        {
            _unsubscriber.Dispose();
        }

        /// <summary>
        /// Message receiving function.
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveOnProducerThread(T msg)
        {
            if (_filter?.PassesProducerThreadFilter(msg) ?? true)
            {
                OnMessageOnProducerThread(msg);
            }
        }

        /// <summary>
        /// Receives message from producer thread.
        /// </summary>
        /// <param name="msg"></param>
        protected void OnMessageOnProducerThread(T msg)
        {
            lock (_batchLock)
            {
                if (!_flushPending)
                {
                    var timerAction = TimerAction.StartNew(() => _batchFiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite);
                    _unsubscriber.BeginSubscriptionAndSetUnsubscriber(timerAction);
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