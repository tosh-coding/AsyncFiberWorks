using System;
using System.Collections.Generic;
using System.Threading;
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
    public class BatchSubscriber<T> : IMessageReceiver<T>, IDisposable
    {
        private readonly object _batchLock = new object();

        private readonly ISubscribableFiber _fiber;
        private readonly Action<IList<T>> _receive;
        private readonly long _intervalInMs;
        private readonly IMessageFilter<T> _filter;

        private List<T> _pending;
        private IDisposable _disposable;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <param name="filter"></param>
        public BatchSubscriber(ISubscribableFiber fiber, Action<IList<T>> receive, long intervalInMs, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _receive = receive;
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
            var unsubscriberFiber = _fiber.CreateSubscription();
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
                    var unsubscriber = _fiber.CreateSubscription();
                    Action cbOnTimerDisposing = () => { unsubscriber.Dispose(); };
                    var timerAction = TimerAction.StartNew(() => _fiber.Enqueue(Flush), _intervalInMs, Timeout.Infinite, cbOnTimerDisposing);
                    unsubscriber.Add(() => timerAction.Dispose());
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