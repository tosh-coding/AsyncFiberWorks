using AsyncFiberWorks.Core;
using System;
using System.Collections.Concurrent;

namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// A channel provides a conduit for messages. It provides methods for publishing and subscribing to messages.
    /// The class is thread safe.
    /// You can specify a channel by specifying a key. This is useful when you
    /// want to use the same message type but with multiple different destinations.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public class Channel<TKey, TMessage> : ISubscriber<TKey, TMessage>, IPublisher<TKey, TMessage>
    {
        private readonly ConcurrentDictionary<TKey, MessageHandlerList<TMessage>> _channels = new ConcurrentDictionary<TKey, MessageHandlerList<TMessage>>();

        private MessageHandlerList<TMessage> GetOrCreateMessageHandlerList(TKey key)
        {
            if (!_channels.TryGetValue(key, out var value))
            {
                value = new MessageHandlerList<TMessage>();
                if (!_channels.TryAdd(key, value))
                {
                    value = _channels[key];
                }
            }
            return (MessageHandlerList<TMessage>)value;
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="key">Key for specifying a channel.</param>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(TKey key, IExecutionContext executionContext, Action<TMessage> receive)
        {
            var channel = GetOrCreateMessageHandlerList(key);
            return channel.AddHandler((msg) => executionContext.Enqueue(() => receive(msg)));
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="key">Key for specifying a channel.</param>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(TKey key, IFiber executionContext, Action<IFiberExecutionEventArgs, TMessage> receive)
        {
            var channel = GetOrCreateMessageHandlerList(key);
            return channel.AddHandler((msg) => executionContext.Enqueue((e) => receive(e, msg)));
        }

        /// <summary>
        /// Publish a message to all subscribers.
        /// </summary>
        /// <param name="key">Key for specifying a channel.</param>
        /// <param name="msg">A message.</param>
        public void Publish(TKey key, TMessage msg)
        {
            var channel = GetOrCreateMessageHandlerList(key);
            channel.Publish(msg);
        }
    }
}
