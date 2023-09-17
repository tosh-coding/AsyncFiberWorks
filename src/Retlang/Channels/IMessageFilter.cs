namespace Retlang.Channels
{
    /// <summary>
    /// Interface for filtering messages.
    /// This is used in the producer/publisher thread before passing it to the subscriber.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageFilter<T>
    {
        /// <summary>
        /// Filter called from producer threads. Should be thread safe as it may be called from
        /// multiple threads.
        /// </summary>
        bool PassesProducerThreadFilter(T msg);
    }
}
