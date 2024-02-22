using System;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscription for actions on a channel.
    /// Subscribe to messages on this channel. The provided action will be invoked via a Action on the provided executor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelSubscription<T> : IMessageReceiver<T>, IDisposable
    {
        private readonly Action<T> _receiver;
        private readonly ISubscribableFiber _fiber;
        private readonly IMessageFilter<T> _filter;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        private bool _started;

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receiver"></param>
        /// <param name="filter"></param>
        public ChannelSubscription(ISubscribableFiber fiber, Action<T> receiver, IMessageFilter<T> filter = null)
        {
            _fiber = fiber;
            _receiver = receiver;
            _filter = filter;
            fiber.BeginSubscriptionAndSetUnsubscriber(_unsubscriber);
        }

        /// <summary>
        /// <see cref="IMessageReceiver.StartSubscription(Channel{T}, Unsubscriber)"/>
        /// </summary>
        public bool StartSubscription(Channel<T> channel, Unsubscriber unsubscriber)
        {
            if (_started)
            {
                unsubscriber.Dispose();
                return false;
            }
            _started = true;

            _unsubscriber.BeginSubscriptionAndSetUnsubscriber(unsubscriber);
            return true;
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
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
            _fiber.Enqueue(() => _receiver(msg));
        }
    }
}