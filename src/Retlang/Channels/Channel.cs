using System;
using System.Collections.Generic;
using Retlang.Core;
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
        /// <see cref="ISubscriber{T}.Subscribe(IFiberWithFallbackRegistry,Action{T})"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IFiberWithFallbackRegistry fiber, Action<T> receive)
        {
            return SubscribeOnProducerThreads(fiber.FallbackDisposer, new ChannelSubscription<T>(fiber, receive));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeToBatch(IFiberWithFallbackRegistry,Action{IList{T}},long)"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        public IDisposable SubscribeToBatch(IFiberWithFallbackRegistry fiber, Action<IList<T>> receive, long intervalInMs)
        {
            return SubscribeOnProducerThreads(fiber.FallbackDisposer, new BatchSubscriber<T>(fiber, receive, intervalInMs, null, fiber.FallbackDisposer));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeToKeyedBatch{K}(IFiberWithFallbackRegistry,Converter{T,K},Action{IDictionary{K,T}},long)"/>
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="fiber"></param>
        /// <param name="keyResolver"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        public IDisposable SubscribeToKeyedBatch<K>(IFiberWithFallbackRegistry fiber, Converter<T, K> keyResolver, Action<IDictionary<K, T>> receive, long intervalInMs)
        {
            return SubscribeOnProducerThreads(fiber.FallbackDisposer, new KeyedBatchSubscriber<K, T>(keyResolver, receive, fiber, intervalInMs, null, fiber.FallbackDisposer));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeToLast(IFiberWithFallbackRegistry,Action{T},long)"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        public IDisposable SubscribeToLast(IFiberWithFallbackRegistry fiber, Action<T> receive, long intervalInMs)
        {
            return SubscribeOnProducerThreads(fiber.FallbackDisposer, new LastSubscriber<T>(receive, fiber, intervalInMs, null, fiber.FallbackDisposer));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeOnProducerThreads(ISubscriptionRegistry,IProducerThreadSubscriber{T})"/>
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public IDisposable SubscribeOnProducerThreads(ISubscriptionRegistry subscriptions, IProducerThreadSubscriber<T> subscriber)
        {
            return SubscribeOnProducerThreads(subscriptions, subscriber.ReceiveOnProducerThread);
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeOnProducerThreads(ISubscriptionRegistry,Action{T})"/>
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="receiveOnProducerThread"></param>
        /// <returns></returns>
        public IDisposable SubscribeOnProducerThreads(ISubscriptionRegistry subscriptions, Action<T> receiveOnProducerThread)
        {
            return _channel.SubscribeOnProducerThreads(subscriptions, receiveOnProducerThread);
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.PersistentSubscribe(IExecutionContext,Action{T})"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        public void PersistentSubscribe(IExecutionContext fiber, Action<T> receive)
        {
            PersistentSubscribeOnProducerThreads(new ChannelSubscription<T>(fiber, receive));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.PersistentSubscribeToBatch(IExecutionContext,Action{IList{T}},long)"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        public void PersistentSubscribeToBatch(IExecutionContext fiber, Action<IList<T>> receive, long intervalInMs)
        {
            PersistentSubscribeOnProducerThreads(new BatchSubscriber<T>(fiber, receive, intervalInMs));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.PersistentSubscribeToKeyedBatch{K}(IExecutionContext,Converter{T,K},Action{IDictionary{K,T}},long)"/>
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="fiber"></param>
        /// <param name="keyResolver"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        public void PersistentSubscribeToKeyedBatch<K>(IExecutionContext fiber, Converter<T, K> keyResolver, Action<IDictionary<K, T>> receive, long intervalInMs)
        {
            PersistentSubscribeOnProducerThreads(new KeyedBatchSubscriber<K, T>(keyResolver, receive, fiber, intervalInMs));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.PersistentSubscribeToLast(IExecutionContext,Action{T},long)"/>
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        public void PersistentSubscribeToLast(IExecutionContext fiber, Action<T> receive, long intervalInMs)
        {
            PersistentSubscribeOnProducerThreads(new LastSubscriber<T>(receive, fiber, intervalInMs));
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.PersistentSubscribeOnProducerThreads(IProducerThreadSubscriber{T})"/>
        /// </summary>
        /// <param name="subscriber"></param>
        public void PersistentSubscribeOnProducerThreads(IProducerThreadSubscriber<T> subscriber)
        {
            PersistentSubscribeOnProducerThreads(subscriber.ReceiveOnProducerThread);
        }

        /// <summary>
        /// <see cref="ISubscriber{T}.PersistentSubscribeOnProducerThreads(Action{T})"/>
        /// </summary>
        /// <param name="receiveOnProducerThread"></param>
        public void PersistentSubscribeOnProducerThreads(Action<T> receiveOnProducerThread)
        {
            _channel.PersistentSubscribeOnProducerThreads(receiveOnProducerThread);
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

        ///<summary>
        /// Number of persistent subscribers.
        ///</summary>
        public int NumPersistentSubscribers { get { return _channel.NumPersistentSubscribers; } }
    }
}