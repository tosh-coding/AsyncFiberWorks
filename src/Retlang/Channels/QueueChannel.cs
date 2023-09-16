using System;
using Retlang.Core;

namespace Retlang.Channels
{
    /// <summary>
    /// Default QueueChannel implementation. Once and only once delivery to first available consumer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueChannel<T>: IQueueChannel<T>
    {
        private readonly InternalQueue<T> _queue = new InternalQueue<T>();
        private event Action _signalEvent;

        /// <summary>
        /// Subscribe to executor messages. 
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="onMessage"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IExecutionContext executionContext, Action<T> onMessage)
        {
            var consumer = new QueueConsumer<T>(executionContext, onMessage, _queue);
            Action signal = consumer.Signal;
            _signalEvent += signal;
            return new Unsubscriber((_) =>
            {
                this._signalEvent -= signal;
            }); 
        }

        /// <summary>
        /// Publish message onto queue. Notify consumers of message.
        /// </summary>
        /// <param name="message"></param>
        public void Publish(T message)
        {
            _queue.Enqueue(message);
            var onSignal = _signalEvent;
            if (onSignal != null)
            {
                onSignal();
            }
        }
    }
}