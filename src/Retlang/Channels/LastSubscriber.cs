using System;
using System.Threading;
using Retlang.Core;

namespace Retlang.Channels
{
    /// <summary>
    /// Subscribes to last action received on the channel. 
    /// Subscription that delivers the latest message to the consuming thread.  If a newer message arrives before the consuming thread
    /// has a chance to process the message, the pending message is replaced by the newer message. The old message is discarded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LastSubscriber<T> : IProducerThreadSubscriber<T>
    {
        private readonly object _batchLock = new object();
        private readonly Action<T> _target;
        private readonly IExecutionContext _fiber;
        private readonly long _intervalInMs;
        private readonly IMessageFilter<T> _filter;
        private readonly ISubscriptionRegistry _fallbackRegistry;

        private bool _flushPending;
        private T _pending;

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fiber"></param>
        /// <param name="intervalInMs"></param>
        /// <param name="filter"></param>
        /// <param name="fallbackRegistry"></param>
        public LastSubscriber(Action<T> target, IExecutionContext fiber, long intervalInMs, IMessageFilter<T> filter = null, ISubscriptionRegistry fallbackRegistry = null)
        {
            _fiber = fiber;
            _target = target;
            _intervalInMs = intervalInMs;
            _filter = filter;
            _fallbackRegistry = fallbackRegistry;
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
        /// Receives message from producer thread.
        /// </summary>
        /// <param name="msg"></param>
        protected void OnMessageOnProducerThread(T msg)
        {
            lock (_batchLock)
            {
                if (!_flushPending)
                {
                    TimerAction.StartNew(() => _fiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite, _fallbackRegistry);
                    _flushPending = true;
                }
                _pending = msg;
            }
        }

        private void Flush()
        {
            _target(ClearPending());
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