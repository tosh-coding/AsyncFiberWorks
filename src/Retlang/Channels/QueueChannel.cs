using System;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Default QueueChannel implementation. Once and only once delivery to first available consumer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueChannel<T>: IQueueChannel<T>
    {
        private readonly InternalQueue<T> _queue = new InternalQueue<T>();
        private readonly InternalChannel<byte> _channel = new InternalChannel<byte>();

        /// <summary>
        /// Subscribe to executor messages. 
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onMessage"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IFiber fiber, Action<T> onMessage)
        {
            var consumer = new QueueConsumer<T>(fiber, onMessage, _queue);
            return _channel.SubscribeOnProducerThreads(fiber, consumer.Signal);
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
    }
}