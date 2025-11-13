namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// Channel publishing interface.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public interface IPublisher<TKey, TMessage>
    {
        /// <summary>
        /// Publish a message to all subscribers.
        /// </summary>
        /// <param name="key">Key for specifying a channel.</param>
        /// <param name="msg">A message.</param>
        void Publish(TKey key, TMessage msg);
    }
}
