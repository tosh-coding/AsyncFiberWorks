using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Queue incoming messages once. It is then dequeued from the callback processing performed on the fiber.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueConsumer<T> : IMessageReceiver<T>
    {
        private readonly object _lock = new object();
        private bool _flushPending;
        private readonly IExecutionContext _fiber;
        private readonly Action<T> _callback;
        private readonly IMessageQueue<T> _queue;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="callback"></param>
        /// <param name="queue"></param>
        public QueueConsumer(IExecutionContext fiber, Action<T> callback, IMessageQueue<T> queue = null)
        {
            if (queue == null)
            {
                queue = new InternalQueue<T>();
            }
            _fiber = fiber;
            _callback = callback;
            _queue = queue;
        }

        /// <summary>
        /// Message receiving function.
        /// </summary>
        /// <param name="msg">A message.</param>
        public void ReceiveOnProducerThread(T msg)
        {
            lock (_lock)
            {
                _queue.Enqueue(msg);
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
                lock (_lock)
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
