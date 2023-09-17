using System;
using System.Collections.Generic;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Batches actions for the consuming thread.
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
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <param name="filter"></param>
        public BatchSubscriber(IFiber fiber, Action<IList<T>> receive, long intervalInMs, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _receive = receive;
            _intervalInMs = intervalInMs;
            _filter = filter;
        }

        ///<summary>
        /// Allows for the registration and deregistration of subscriptions
        ///</summary>
        public ISubscriptionRegistry Subscriptions
        {
            get { return _fiber; }
        }

        /// <summary>
        /// <see cref="IProducerThreadSubscriberCore{T}.ReceiveOnProducerThread"/>
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
                    TimerAction.StartNew(_fiber, Flush, _intervalInMs);
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