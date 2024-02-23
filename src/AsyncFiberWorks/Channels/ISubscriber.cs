using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Channel subscription interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriber<T>
    {
        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="messageReceiver">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposableSubscriptionRegistry Subscribe(IMessageReceiver<T> messageReceiver);
    }
}
