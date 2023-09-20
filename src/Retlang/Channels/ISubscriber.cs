using System;
using System.Collections.Generic;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// Channel subscription methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriber<T>
    {
        ///<summary>
        /// Subscribe to messages on this channel. The provided action will be invoked via a Action on the provided executor.
        ///</summary>
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="receive"></param>
        ///<returns>Unsubscriber object</returns>
        IDisposable Subscribe(IFiber fiber, Action<T> receive);

        /// <summary>
        /// Subscribes to actions on the channel in batch form. The events will be batched if the consumer is unable to process the events 
        /// faster than the arrival rate.
        /// </summary>
        /// <param name="fiber">The target context to execute the action</param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        /// <returns></returns>
        IDisposable SubscribeToBatch(IFiber fiber, Action<IList<T>> receive, long intervalInMs);

        ///<summary>
        /// Batches actions based upon keyed values allowing for duplicates to be dropped. 
        ///</summary>
        ///<param name="fiber"></param>
        ///<param name="keyResolver"></param>
        ///<param name="receive"></param>
        ///<param name="intervalInMs"></param>
        ///<typeparam name="K"></typeparam>
        ///<returns></returns>
        IDisposable SubscribeToKeyedBatch<K>(IFiber fiber, Converter<T, K> keyResolver, Action<IDictionary<K, T>> receive, long intervalInMs);

        /// <summary>
        /// Subscription that delivers the latest message to the consuming thread.  If a newer message arrives before the consuming thread
        /// has a chance to process the message, the pending message is replaced by the newer message. The old message is discarded.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        /// <returns></returns>
        IDisposable SubscribeToLast(IFiber fiber, Action<T> receive, long intervalInMs);

        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        IDisposable SubscribeOnProducerThreads(ISubscriptionRegistryGetter subscriptions, IProducerThreadSubscriber<T> subscriber);

        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="receiveOnProducerThread">A message receive process that is performed on the producer/publisher thread. Probably just transfer it to another fiber.</param>
        /// <returns></returns>
        IDisposable SubscribeOnProducerThreads(ISubscriptionRegistryGetter subscriptions, Action<T> receiveOnProducerThread);

        ///<summary>
        /// Subscribe to messages on this channel. The provided action will be invoked via a Action on the provided executor.
        /// This subscription cannot be unsubscribed. The subscriber must be valid until this channel is destroyed.
        ///</summary>
        ///<param name="fiber"></param>
        ///<param name="receive"></param>
        void PersistentSubscribe(IFiber fiber, Action<T> receive);

        /// <summary>
        /// Subscribes to actions on the channel in batch form. The events will be batched if the consumer is unable to process the events 
        /// faster than the arrival rate.
        /// This subscription cannot be unsubscribed. The subscriber must be valid until this channel is destroyed.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs">Time in Ms to batch actions. If 0 events will be delivered as fast as consumer can process</param>
        void PersistentSubscribeToBatch(IFiber fiber, Action<IList<T>> receive, long intervalInMs);

        ///<summary>
        /// Batches actions based upon keyed values allowing for duplicates to be dropped. 
        /// This subscription cannot be unsubscribed. The subscriber must be valid until this channel is destroyed.
        ///</summary>
        ///<param name="fiber"></param>
        ///<param name="keyResolver"></param>
        ///<param name="receive"></param>
        ///<param name="intervalInMs"></param>
        ///<typeparam name="K"></typeparam>
        void PersistentSubscribeToKeyedBatch<K>(IFiber fiber, Converter<T, K> keyResolver, Action<IDictionary<K, T>> receive, long intervalInMs);

        /// <summary>
        /// Subscription that delivers the latest message to the consuming thread.  If a newer message arrives before the consuming thread
        /// has a chance to process the message, the pending message is replaced by the newer message. The old message is discarded.
        /// This subscription cannot be unsubscribed. The subscriber must be valid until this channel is destroyed.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="intervalInMs"></param>
        void PersistentSubscribeToLast(IFiber fiber, Action<T> receive, long intervalInMs);

        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// This subscription cannot be unsubscribed. The subscriber must be valid until this channel is destroyed.
        /// </summary>
        /// <param name="subscriber"></param>
        void PersistentSubscribeOnProducerThreads(IProducerThreadSubscriber<T> subscriber);

        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// This subscription cannot be unsubscribed. The subscriber must be valid until this channel is destroyed.
        /// </summary>
        /// <param name="receiveOnProducerThread">A message receive process that is performed on the producer/publisher thread. Probably just transfer it to another fiber.</param>
        void PersistentSubscribeOnProducerThreads(Action<T> receiveOnProducerThread);
    }
}
