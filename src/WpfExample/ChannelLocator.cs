using AsyncFiberWorks.PubSub;
using System;
using System.Collections.Generic;

namespace WpfExample
{
    public static class ChannelLocator
    {
        private static readonly Dictionary<Type, object> _channels = new Dictionary<Type, object>();

        public static ISubscriber<T> GetSubscriber<T>()
        {
            if (!_channels.TryGetValue(typeof(T), out var value))
            {
                value = new Channel<T>();
                _channels[typeof(T)] = value;
            }
            var channel = (Channel<T>)value;
            return channel;
        }

        public static IPublisher<T> GetPublisher<T>()
        {
            if (!_channels.TryGetValue(typeof(T), out var value))
            {
                value = new Channel<T>();
                _channels[typeof(T)] = value;
            }
            var channel = (Channel<T>)value;
            return channel;
        }
    }
}