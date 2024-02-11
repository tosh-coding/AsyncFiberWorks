using System;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Subscription for actions on a channel.
    /// Subscribe to messages on this channel. The provided action will be invoked via a Action on the provided executor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelSubscription<T>
    {
        private readonly Action<T> _receiver;
        private readonly IFiber _fiber;
        private readonly IMessageFilter<T> _filter;

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receiver"></param>
        /// <param name="filter"></param>
        public ChannelSubscription(IFiber fiber, Action<T> receiver, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _receiver = receiver;
            _filter = filter;
        }

        /// <summary>
        /// Start subscribing to the channel.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        /// <returns>For unsubscriptions.</returns>
        public IDisposable Subscribe(ISubscriber<T> channel)
        {
            var disposable = channel.SubscribeOnProducerThreads(ReceiveOnProducerThread);
            return _fiber.FallbackDisposer?.RegisterSubscriptionAndCreateDisposable(disposable) ?? disposable;
        }

        /// <summary>
        /// Message receiving function.
        /// </summary>
        /// <param name="msg"></param>
        void ReceiveOnProducerThread(T msg)
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