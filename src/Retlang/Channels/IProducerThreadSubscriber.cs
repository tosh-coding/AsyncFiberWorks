namespace Retlang.Channels
{
    /// <summary>
    /// A subscriber with a receive function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IProducerThreadSubscriber<T>
    {
        /// <summary>
        /// Message receiving function.
        /// It runs on producer/publisher thread and will forward messages to other fibers.
        /// </summary>
        /// <param name="msg"></param>
        void ReceiveOnProducerThread(T msg);
    }
}
