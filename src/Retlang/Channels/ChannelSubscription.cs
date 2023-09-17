using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Subscription for actions on a channel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelSubscription<T> : ISubscriberWithFilter<T>
    {
        private readonly Action<T> _receiver;
        private readonly IFiber _fiber;
        private readonly MessageFilter<T> _filter = new MessageFilter<T>();

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receiver"></param>
        public ChannelSubscription(IFiber fiber, Action<T> receiver)
        {
            _fiber = fiber;
            _receiver = receiver;
        }

        ///<summary>
        /// Allows for the registration and deregistration of subscriptions
        ///</summary>
        public ISubscriptionRegistry Subscriptions
        {
            get { return _fiber; }
        }

        /// <summary>
        /// <see cref="IMessageFilter{T}.FilterOnProducerThread"/>
        /// </summary>
        public Filter<T> FilterOnProducerThread
        {
            get { return _filter.FilterOnProducerThread; }
            set { _filter.FilterOnProducerThread = value; }
        }

        /// <summary>
        /// <see cref="IProducerThreadSubscriberCore{T}.ReceiveOnProducerThread"/>
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveOnProducerThread(T msg)
        {
            if (_filter.PassesProducerThreadFilter(msg))
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