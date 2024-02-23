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
    }
}
