using AsyncFiberWorks.Core;

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
        /// Begin a subscription. Then set its unsubscriber to disposable.
        /// </summary>
        /// <param name="disposable">Disposables that can be reserved for unsubscriptions.</param>
        void BeginSubscriptionAndSetUnsubscriber(IDisposableSubscriptionRegistry disposable);
    }
}
