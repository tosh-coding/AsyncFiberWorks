using System.Collections.Generic;

namespace Retlang.Channels
{
    internal class InternalQueue<T> : IMessageQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T message)
        {
            lock (_queue)
            {
                _queue.Enqueue(message);
            }
        }

        public bool Pop(out T msg)
        {
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    msg = _queue.Dequeue();
                    return true;
                }
            }
            msg = default(T);
            return false;
        }

        public bool IsEmpty
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count <= 0;
                }
            }
        }
    }
}
