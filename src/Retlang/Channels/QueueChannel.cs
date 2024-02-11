using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Default QueueChannel implementation. Once and only once delivery to first available consumer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueChannel<T>: IQueueChannel<T>
    {
        private readonly IMessageQueue<T> _queue;
        private readonly InternalChannel<byte> _channel = new InternalChannel<byte>();

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
        /// <param name="fiber"></param>
        /// <param name="onMessage"></param>
        /// <param name="registry"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IExecutionContext fiber, Action<T> onMessage, ISubscriptionRegistry registry)
        {
            var consumer = new QueueConsumer<T>(fiber, onMessage, _queue);
            var disposable = _channel.SubscribeOnProducerThreads(consumer.Signal);
            return registry?.RegisterSubscriptionAndCreateDisposable(disposable) ?? disposable;
        }

        /// <summary>
        /// Subscribe to executor messages. 
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="onMessage"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IFiberWithFallbackRegistry fiber, Action<T> onMessage)
        {
            return Subscribe(fiber, onMessage, fiber.FallbackDisposer);
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
        public int NumSubscribers { get { return _channel.NumSubscribers; } }
    }
}
