using System;
using System.Collections.Generic;
using System.Threading;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Batches actions for the consuming thread.
    /// Subscribes to actions on the channel in batch form. The events will be batched if the consumer is unable to process the events 
    /// faster than the arrival rate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BatchSubscriber<T> : IProducerThreadSubscriber<T>
    {
        private readonly object _batchLock = new object();

        private readonly IFiber _fiber;
        private readonly Action<IList<T>> _receive;
        private readonly long _intervalInMs;
        private readonly IMessageFilter<T> _filter;

        private List<T> _pending;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="filter"></param>
        public BatchSubscriber(IFiber fiber, Action<IList<T>> receive, long intervalInMs, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _receive = receive;
            _intervalInMs = intervalInMs;
            _filter = filter;
        }

        /// <summary>
        /// Start subscribing to the channel.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        /// <returns>For unsubscriptions.</returns>
        public IDisposable Subscribe(ISubscriber<T> channel)
        {
            var disposable = channel.SubscribeOnProducerThreads(this);
            return _fiber.FallbackDisposer?.RegisterSubscriptionAndCreateDisposable(disposable) ?? disposable;
        }

        /// <summary>
        /// <see cref="IProducerThreadSubscriber{T}.ReceiveOnProducerThread"/>
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
        /// Receives message and batches as needed.
        /// </summary>
        /// <param name="msg"></param>
        protected void OnMessageOnProducerThread(T msg)
        {
            lock (_batchLock)
            {
                if (_pending == null)
                {
                    _pending = new List<T>();
                    TimerAction.StartNew(() => _fiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite, _fiber.FallbackDisposer);
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
                _receive(toFlush);
            }
        }
    }
}