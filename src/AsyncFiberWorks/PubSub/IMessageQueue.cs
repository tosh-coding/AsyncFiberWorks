namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// An interface for the queue operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageQueue<T>
    {
        /// <summary>
        /// Add a message to the queue.
        /// </summary>
        /// <param name="message"></param>
        void Enqueue(T message);

        /// <summary>
        /// Take a message from the queue.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool Pop(out T msg);

        /// <summary>
        /// The queue is empty.
        /// </summary>
        bool IsEmpty { get; }
    }
}
