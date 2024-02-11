using System;
using System.Collections.Generic;
using System.Threading;
using Retlang.Core;

namespace Retlang.Channels
{
    /// <summary>
    /// Channel subscription that drops duplicates based upon a key.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="T"></typeparam>
    public class KeyedBatchSubscriber<K, T> : IProducerThreadSubscriber<T>
    {
        private readonly object _batchLock = new object();

        private readonly Action<IDictionary<K, T>> _target;
        private readonly Converter<T, K> _keyResolver;
        private readonly IExecutionContext _fiber;
        private readonly long _intervalInMs;
        private readonly IMessageFilter<T> _filter;
        private readonly ISubscriptionRegistry _fallbackRegistry;

        private Dictionary<K, T> _pending;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver"></param>
        /// <param name="target"></param>
        /// <param name="fiber"></param>
        /// <param name="intervalInMs"></param>
        /// <param name="filter"></param>
        /// <param name="fallbackRegistry"></param>
        public KeyedBatchSubscriber(Converter<T, K> keyResolver, Action<IDictionary<K, T>> target, IExecutionContext fiber, long intervalInMs, IMessageFilter<T> filter = null, ISubscriptionRegistry fallbackRegistry = null)
        {
            _keyResolver = keyResolver;
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
                    TimerAction.StartNew(() => _fiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite, _fallbackRegistry);
                }
                _pending[key] = msg;
            }
        }

        private void Flush()
        {
            var toReturn = ClearPending();
            if (toReturn != null)
            {
                _target(toReturn);
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