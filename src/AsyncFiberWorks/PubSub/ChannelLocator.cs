using System;
using System.Collections.Concurrent;

namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// Interface for getting channels.
    /// </summary>
    public static class ChannelLocator
    {
        private static readonly ConcurrentDictionary<Type, object> _channels = new ConcurrentDictionary<Type, object>();

        static Channel<T> GetOrCreateChannel<T>()
        {
            var t = typeof(T);
            if (!_channels.TryGetValue(t, out var value))
            {
                value = new Channel<T>();
                if (!_channels.TryAdd(t, value))
                {
                    value = _channels[t];
                }
            }
            return (Channel<T>)value;
        }

        /// <summary>
        /// Get the unique instance in the process using the type as the key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The publishing interface of the found channel.</returns>
        public static ISubscriber<T> GetSubscriber<T>()
        {
            return GetOrCreateChannel<T>();
        }

        /// <summary>
        /// Get the unique instance in the process using the type as the key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>A subscription interface for the found channels.</returns>
        public static IPublisher<T> GetPublisher<T>()
        {
            return GetOrCreateChannel<T>();
        }

        static Channel<TKey, TMessage> GetOrCreateKeyedChannel<TKey, TMessage>()
        {
            return KeyedChannelHolder<TKey, TMessage>._channel;
        }

        /// <summary>
        /// Get a subscription interface.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns>The interface found.</returns>
        public static ISubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>()
        {
            return KeyedChannelHolder<TKey, TMessage>._channel;
        }

        /// <summary>
        /// Get a publishing interface.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns>The interface found.</returns>
        public static IPublisher<TKey, TMessage> GetPublisher<TKey, TMessage>()
        {
            return GetOrCreateKeyedChannel<TKey, TMessage>();
        }

        internal static class KeyedChannelHolder<TKey, TMessage>
        {
            internal static readonly Channel<TKey, TMessage> _channel = new Channel<TKey, TMessage>();
        }
    }
}
