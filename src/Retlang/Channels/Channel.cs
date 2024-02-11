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
        /// <see cref="ISubscriber{T}.SubscribeOnProducerThreads(Action{T})"/>
        /// </summary>
        /// <param name="receiveOnProducerThread"></param>
        /// <returns></returns>
        public IDisposable SubscribeOnProducerThreads(Action<T> receiveOnProducerThread)
        {
            return _channel.SubscribeOnProducerThreads(receiveOnProducerThread);
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
