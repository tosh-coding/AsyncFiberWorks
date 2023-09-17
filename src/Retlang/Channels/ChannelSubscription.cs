using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Subscription for actions on a channel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelSubscription<T> : IProducerThreadSubscriber<T>
    {
        private readonly Action<T> _receiver;
        private readonly IFiber _fiber;
        private readonly IMessageFilter<T> _filter;

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receiver"></param>
        /// <param name="filter"></param>
        public ChannelSubscription(IFiber fiber, Action<T> receiver, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _receiver = receiver;
            _filter = filter;
        }

        /// <summary>
        /// <see cref="IProducerThreadSubscriber{T}.ReceiveOnProducerThread"/>
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
            _fiber.Enqueue(() => _receiver(msg));
        }
    }
}