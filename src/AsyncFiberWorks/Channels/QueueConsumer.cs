using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    internal class QueueConsumer<T> : IDisposableSubscriptionRegistry, IDisposable
    {
        private bool _flushPending;
        private readonly IExecutionContext _fiber;
        private readonly Action<T> _callback;
        private readonly IMessageQueue<T> _queue;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        public QueueConsumer(IExecutionContext fiber, Action<T> callback, IMessageQueue<T> queue)
        {
            _fiber = fiber;
            _callback = callback;
            _queue = queue;
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
        }

        public void AddDisposable(IDisposable disposable)
        {
            _unsubscriber.Add(() => disposable.Dispose());
        }

        /// <summary>
        /// <see cref="IDisposableSubscriptionRegistry.BeginSubscription"/>
        /// </summary>
        public Unsubscriber BeginSubscription()
        {
            return _unsubscriber.BeginSubscription();
        }

        public void Signal(byte dummy)
        {
            lock (this)
            {
                if (_flushPending)
                {
                    return;
                }
                _fiber.Enqueue(ConsumeNext);
                _flushPending = true;
            }
        }

        private void ConsumeNext()
        {
            try
            {
                T msg;
                if (_queue.Pop(out msg))
                {
                    _callback(msg);
                }
            }
            finally
            {
                lock (this)
                {
                    if (_queue.IsEmpty)
                    {
                        _flushPending = false;
                    }
                    else
                    {
                        _fiber.Enqueue(ConsumeNext);
                    }
                }
            }
        }
    }
}
