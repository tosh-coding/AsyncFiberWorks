using System;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    internal class QueueConsumer<T> : IDisposable
    {
        private bool _flushPending;
        private readonly ISubscribableFiber _fiber;
        private readonly Action<T> _callback;
        private readonly IMessageQueue<T> _queue;
        private IDisposable _disposable;

        public QueueConsumer(ISubscribableFiber fiber, Action<T> callback, IMessageQueue<T> queue)
        {
            _fiber = fiber;
            _callback = callback;
            _queue = queue;
        }

        public void Dispose()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
        }

        public void SetDisposable(IDisposable disposable)
        {
            _disposable = disposable;
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
