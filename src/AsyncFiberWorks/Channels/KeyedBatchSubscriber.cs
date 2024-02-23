using System;
using System.Collections.Generic;
using System.Threading;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Channel subscription that drops duplicates based upon a key.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="T"></typeparam>
    public class KeyedBatchSubscriber<K, T> : IMessageReceiver<T>, IDisposable
    {
        private readonly object _batchLock = new object();

        private readonly IMessageFilter<T> _filter;
        private readonly Converter<T, K> _keyResolver;
        private readonly long _intervalInMs;
        private readonly IExecutionContext _batchFiber;
        private readonly ISubscribableFiber _executeFiber;
        private readonly Action<IDictionary<K, T>> _receive;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        private Dictionary<K, T> _pending;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchSubscriber(Converter<T, K> keyResolver, long intervalInMs, Action<IDictionary<K, T>> receive)
            : this(keyResolver, intervalInMs, null, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchSubscriber(Converter<T, K> keyResolver, long intervalInMs, ISubscribableFiber fiber, Action<IDictionary<K, T>> receive)
            : this(null, keyResolver, intervalInMs, fiber, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        /// <param name="keyResolver"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchSubscriber(IMessageFilter<T> filter, Converter<T, K> keyResolver, long intervalInMs, ISubscribableFiber fiber, Action<IDictionary<K, T>> receive)
            : this(filter, keyResolver, intervalInMs, null, fiber, receive)
        {
        }

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        /// <param name="keyResolver"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="batchFiber">Fiber used for batch processing.</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public KeyedBatchSubscriber(IMessageFilter<T> filter, Converter<T, K> keyResolver, long intervalInMs, IExecutionContext batchFiber, ISubscribableFiber fiber, Action<IDictionary<K, T>> receive)
        {
            _filter = filter;
            _keyResolver = keyResolver;
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
        /// received on delivery thread
        /// </summary>
        /// <param name="msg"></param>
        protected void OnMessageOnProducerThread(T msg)
        {
            lock (_batchLock)
            {
                var key = _keyResolver(msg);
                if (_pending == null)
                {
                    _pending = new Dictionary<K, T>();
                    var timerAction = TimerAction.StartNew(() => _batchFiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite);
                    _unsubscriber.BeginSubscriptionAndSetUnsubscriber(timerAction);
                }
                _pending[key] = msg;
            }
        }

        private void Flush()
        {
            var toReturn = ClearPending();
            if (toReturn != null)
            {
                if ((_executeFiber != null) && (_batchFiber != _executeFiber))
                {
                    _executeFiber.Enqueue(() => _receive(toReturn));
                }
                else
                {
                    _receive(toReturn);
                }
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