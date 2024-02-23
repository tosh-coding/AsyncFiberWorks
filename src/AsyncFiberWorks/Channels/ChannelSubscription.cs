using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscription for actions on a channel.
    /// Subscribe to messages on this channel. The provided action will be invoked via a Action on the provided executor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelSubscription<T> : IMessageReceiver<T>
    {
        private readonly IMessageFilter<T> _filter;
        private readonly IExecutionContext _fiber;
        private readonly Action<T> _receive;

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="receive">Message receiving handler.</param>
        public ChannelSubscription(Action<T> receive)
            : this(null, receive)
        {
        }

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public ChannelSubscription(IExecutionContext fiber, Action<T> receive)
            : this(null, fiber, receive)
        {
        }

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public ChannelSubscription(IMessageFilter<T> filter, IExecutionContext fiber, Action<T> receive)
        {
            _filter = filter;
            _fiber = fiber;
            _receive = receive;
        }

        /// <summary>
        /// Message receiving function.
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveOnProducerThread(T msg)
        {
            if (_filter?.PassesProducerThreadFilter(msg) ?? true)
            {
                OnMessageOnProducerThread(msg);
            }
        }

        /// <summary>
        /// Receives the action and queues the execution on the target fiber.
        /// </summary>
        /// <param name="msg"></param>
        protected void OnMessageOnProducerThread(T msg)
        {
            if (_fiber != null)
            {
                _fiber.Enqueue(() => _receive(msg));
            }
            else
            {
                _receive(msg);
            }
        }
    }
}