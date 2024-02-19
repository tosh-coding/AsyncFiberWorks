using System;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Default QueueChannel implementation. Once and only once delivery to first available consumer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueChannel<T>: IQueueChannel<T>
    {
        private readonly IMessageQueue<T> _queue;
        private readonly MessageHandlerList<byte> _channel = new MessageHandlerList<byte>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="queue"></param>
        public QueueChannel(IMessageQueue<T> queue = null)
        {
            if (queue == null)
            {
                queue = new InternalQueue<T>();
            }
            _queue = queue;
        }

        /// <summary>
        /// Subscribe to executor messages. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="outQueue"></param>
        /// <returns></returns>
        public IDisposable OnSubscribe(Action<byte> action, out IMessageQueue<T> outQueue)
        {
            outQueue = _queue;
            return _channel.AddHandler(action);
        }

        /// <summary>
        /// Publish message onto queue. Notify consumers of message.
        /// </summary>
        /// <param name="message"></param>
        public void Publish(T message)
        {
            _queue.Enqueue(message);
            _channel.Publish(default);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _channel.Count; } }
    }
}
