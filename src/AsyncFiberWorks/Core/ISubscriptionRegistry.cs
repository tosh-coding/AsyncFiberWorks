using AsyncFiberWorks.Channels;

namespace AsyncFiberWorks.Core
{
    ///<summary>
    /// Allows for the registration and deregistration of subscriptions
    ///</summary>
    public interface ISubscriptionRegistry
    {
        /// <summary>
        /// Begin subscription.
        /// </summary>
        /// <returns>Unsubscribers. It is also discarded when the subscription subject is terminated.</returns>
        Unsubscriber BeginSubscription();
    }
}