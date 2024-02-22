using AsyncFiberWorks.Channels;

namespace AsyncFiberWorks.Core
{
    ///<summary>
    /// Allows for the registration and deregistration of subscriptions
    ///</summary>
    public interface ISubscriptionRegistry
    {
        /// <summary>
        /// Create an unsubscriber who unsubscribes when the fiber is discarded.
        /// </summary>
        /// <returns>Unsubscriber with the unregister process as an element.</returns>
        Unsubscriber BeginSubscription();

        /// <summary>
        /// Begin a subscription. Then set its unsubscriber to disposable.
        /// </summary>
        /// <param name="disposable">Disposables that can be reserved for unsubscriptions.</param>
        void BeginSubscriptionAndSetUnsubscriber(IDisposableSubscriptionRegistry disposable);
    }
}