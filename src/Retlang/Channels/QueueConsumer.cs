using System;
using Retlang.Fibers;

namespace Retlang.Channels
{
    internal class QueueConsumer<T> : IDisposable
    {
        private bool _flushPending;
        private readonly IFiberWithFallbackRegistry _fiber;
        private readonly Action<T> _callback;
        private IMessageQueue<T> _queue;
        private IDisposable _disposable;

        public QueueConsumer(IFiberWithFallbackRegistry fiber, Action<T> callback)
        {
            _fiber = fiber;
            _callback = callback;
            _queue = null;
        }

        public void Subscribe(QueueChannel<T> channel)
        {
            var disposable = channel.OnSubscribe(Signal, out _queue);
            var unsubscriber = _fiber.CreateSubscription();
            if (unsubscriber != null)
            {
                unsubscriber.Add(() => disposable.Dispose());
            }
            _disposable = unsubscriber ?? disposable;
        }

        public void Dispose()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
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
