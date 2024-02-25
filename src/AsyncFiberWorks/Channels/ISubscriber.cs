using System;

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
        /// <param name="receiveOnProducerThread">Subscriber.</param>
        /// <returns></returns>
        IDisposable Subscribe(Action<T> receiveOnProducerThread);

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="messageReceiver">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(IMessageReceiver<T> messageReceiver);
    }
}
