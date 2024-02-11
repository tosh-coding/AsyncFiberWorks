using System;
using Retlang.Fibers;

namespace Retlang.Channels
{
    internal class QueueConsumer<T>
    {
        private bool _flushPending;
        private readonly IFiberWithFallbackRegistry _fiber;
        private readonly Action<T> _callback;
        private IMessageQueue<T> _queue;

        public QueueConsumer(IFiberWithFallbackRegistry fiber, Action<T> callback)
        {
            _fiber = fiber;
            _callback = callback;
            _queue = null;
        }

        public IDisposable Subscribe(QueueChannel<T> channel)
        {
            var disposable = channel.OnSubscribe(Signal, out _queue);
            return _fiber.FallbackDisposer?.RegisterSubscriptionAndCreateDisposable(disposable) ?? disposable;
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
