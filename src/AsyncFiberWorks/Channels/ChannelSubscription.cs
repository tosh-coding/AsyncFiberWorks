using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscription for actions on a channel.
    /// Subscribe to messages on this channel. The provided action will be invoked via a Action on the provided executor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelSubscription<T>
    {
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
        {
            _fiber = fiber;
            _receive = receive;
        }

        /// <summary>
        /// Receives the action and queues the execution on the target fiber.
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveOnProducerThread(T msg)
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