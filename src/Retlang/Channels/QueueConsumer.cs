using System;
using Retlang.Core;

namespace Retlang.Channels
{
    internal class QueueConsumer<T>
    {
        private bool _flushPending;
        private readonly IExecutionContext _target;
        private readonly Action<T> _callback;
        private readonly IMessageQueue<T> _queue;

        public QueueConsumer(IExecutionContext target, Action<T> callback, IMessageQueue<T> queue)
        {
            _target = target;
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
                _target.Enqueue(ConsumeNext);
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
                        _target.Enqueue(ConsumeNext);
                    }
                }
            }
        }
    }
}
