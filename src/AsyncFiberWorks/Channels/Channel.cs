using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    ///<summary>
    /// Default Channel Implementation. Methods are thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class Channel<T> : IChannel<T>
    {
        private readonly MessageHandlerList<T> _channel = new MessageHandlerList<T>();

        /// <summary>
        /// <see cref="ISubscriber{T}.SubscribeOnProducerThreads(Action{T})"/>
        /// </summary>
        /// <param name="receiveOnProducerThread"></param>
        /// <returns></returns>
        public IDisposable SubscribeOnProducerThreads(Action<T> receiveOnProducerThread)
        {
            return _channel.AddHandler(receiveOnProducerThread);
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
        public int NumSubscribers { get { return _channel.Count; } }
    }
}
