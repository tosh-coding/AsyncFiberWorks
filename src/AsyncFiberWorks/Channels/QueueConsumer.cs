using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    internal class QueueConsumer<T>
    {
        private bool _flushPending;
        private readonly IExecutionContext _fiber;
        private readonly Action<T> _callback;
        private readonly IMessageQueue<T> _queue;

        public QueueConsumer(IExecutionContext fiber, Action<T> callback, IMessageQueue<T> queue)
        {
            _fiber = fiber;
            _callback = callback;
            _queue = queue;
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
