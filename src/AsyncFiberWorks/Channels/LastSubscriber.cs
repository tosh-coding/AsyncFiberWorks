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
        private readonly Action<T> _target;
        private readonly ISubscribableFiber _fiber;
        private readonly long _intervalInMs;
        private readonly IMessageFilter<T> _filter;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        private bool _flushPending;
        private T _pending;

        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fiber"></param>
        /// <param name="intervalInMs"></param>
        /// <param name="filter"></param>
        public LastSubscriber(Action<T> target, ISubscribableFiber fiber, long intervalInMs, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _target = target;
            _intervalInMs = intervalInMs;
            _filter = filter;
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
                    var timerAction = TimerAction.StartNew(() => _fiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite);
                    _unsubscriber.BeginSubscriptionAndSetUnsubscriber(timerAction);
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