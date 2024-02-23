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
        private readonly IMessageFilter<T> _filter;
        private readonly ISubscribableFiber _fiber;
        private readonly Action<T> _receive;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

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
        public ChannelSubscription(ISubscribableFiber fiber, Action<T> receive)
            : this(null, fiber, receive)
        {
        }

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public ChannelSubscription(IMessageFilter<T> filter, ISubscribableFiber fiber, Action<T> receive)
        {
            _filter = filter;
            _fiber = fiber;
            _receive = receive;
            fiber.BeginSubscriptionAndSetUnsubscriber(_unsubscriber);
        }

        /// <summary>
        /// <see cref="IMessageReceiver{T}.AddDisposable(IDisposable)"/>
        /// </summary>
        /// <param name="disposable"></param>
        public void AddDisposable(IDisposable disposable)
        {
            _unsubscriber.Add(() => disposable.Dispose());
        }

        /// <summary>
        /// Unsubscribe the fiber. Also discards added disposables.
        /// </summary>
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