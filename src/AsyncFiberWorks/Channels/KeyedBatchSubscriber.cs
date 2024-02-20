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

        private readonly Action<IDictionary<K, T>> _target;
        private readonly Converter<T, K> _keyResolver;
        private readonly ISubscribableFiber _fiber;
        private readonly long _intervalInMs;
        private readonly IMessageFilter<T> _filter;

        private Dictionary<K, T> _pending;
        private IDisposable _disposable;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="keyResolver"></param>
        /// <param name="target"></param>
        /// <param name="fiber"></param>
        /// <param name="intervalInMs"></param>
        /// <param name="filter"></param>
        public KeyedBatchSubscriber(Converter<T, K> keyResolver, Action<IDictionary<K, T>> target, ISubscribableFiber fiber, long intervalInMs, IMessageFilter<T> filter = null)
        {
            _keyResolver = keyResolver;
            _fiber = fiber;
            _target = target;
            _intervalInMs = intervalInMs;
            _filter = filter;
        }

        /// <summary>
        /// <see cref="IMessageReceiver.StartSubscription(Channel{T}, Unsubscriber)"/>
        /// </summary>
        public bool StartSubscription(Channel<T> channel, Unsubscriber unsubscriber)
        {
            if (_disposable != null)
            {
                unsubscriber.Dispose();
                return false;
            }
            var unsubscriberFiber = _fiber.BeginSubscription();
            if (unsubscriberFiber != null)
            {
                unsubscriberFiber.Add(() => unsubscriber.Dispose());
            }
            _disposable = unsubscriberFiber ?? unsubscriber;
            return true;
        }

        public void Dispose()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
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
                    var unsubscriber = _fiber.BeginSubscription();
                    Action cbOnTimerDisposing = () => { unsubscriber.Dispose(); };
                    var timerAction = TimerAction.StartNew(() => _fiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite, cbOnTimerDisposing);
                    unsubscriber.Add(() => timerAction.Dispose());
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