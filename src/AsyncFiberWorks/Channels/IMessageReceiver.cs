namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Message receiving function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageReceiver<T>
    {
        /// <summary>
        /// A message receive process that is performed on the producer/publisher thread.
        /// Probably just transfer it to another fiber.
        /// </summary>
        void ReceiveOnProducerThread(T msg);

        /// <summary>
        /// Start subscription.
        /// This is called from the subscription start process of the channel class.
        /// </summary>
        /// <param name="channel">The channel in the subscription start process.</param>
        /// <param name="unsubscriber">An unsubscription to a channel.</param>
        /// <returns>Success or failure.</returns>
        bool StartSubscription(Channel<T> channel, Unsubscriber unsubscriber);
    }
}
