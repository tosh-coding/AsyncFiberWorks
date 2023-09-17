using System;
using System.Collections.Generic;
using Retlang.Fibers;

namespace Retlang.Channels
{
    ///<summary>
    /// Default Channel Implementation. Methods are thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class Channel<T> : IChannel<T>
    {
        private readonly InternalChannel<T> _channel = new InternalChannel<T>();

        /// <summary>
        /// <see cref="ISubscriber{T}.Subscribe(IFiber,Action{T})"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IFiber fiber, Action<T> receive)
        {
            return SubscribeOnProducerThreads(fiber, new ChannelSubscription<T>(fiber, receive));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeToBatch(IFiber,Action{IList{T}},long)"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        public IDisposable SubscribeToBatch(IFiber fiber, Action<IList<T>> receive, long intervalInMs)
        {
            return SubscribeOnProducerThreads(fiber, new BatchSubscriber<T>(fiber, receive, intervalInMs));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeToKeyedBatch{K}(IFiber,Converter{T,K},Action{IDictionary{K,T}},long)"/>
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="fiber"></param>
        /// <param name="keyResolver"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        public IDisposable SubscribeToKeyedBatch<K>(IFiber fiber, Converter<T, K> keyResolver, Action<IDictionary<K, T>> receive, long intervalInMs)
        {
            return SubscribeOnProducerThreads(fiber, new KeyedBatchSubscriber<K, T>(keyResolver, receive, fiber, intervalInMs));
        }

        /// <summary>
        /// Subscription that delivers the latest message to the consuming thread.  If a newer message arrives before the consuming thread
        /// has a chance to process the message, the pending message is replaced by the newer message. The old message is discarded.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        public IDisposable SubscribeToLast(IFiber fiber, Action<T> receive, long intervalInMs)
        {
            return SubscribeOnProducerThreads(fiber, new LastSubscriber<T>(receive, fiber, intervalInMs));
        }

        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public IDisposable SubscribeOnProducerThreads(IFiber fiber, IProducerThreadSubscriber<T> subscriber)
        {
            return _channel.SubscribeOnProducerThreads(fiber, subscriber.ReceiveOnProducerThread);
        }

        /// <summary>
        /// <see cref="IPublisher{T}.Publish(T)"/>
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Publish(T msg)
        {
            return _channel.Publish(msg);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _channel.NumSubscribers; } }
    }
}