namespace Retlang.Channels
{
    /// <summary>
    /// The interface for QueueChannel publishers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPublisherQueueChannel<T>
    {
        /// <summary>
        /// Pushes a message into the queue. Message will be processed by first available consumer.
        /// </summary>
        /// <param name="message"></param>
        void Publish(T message);
    }
}
