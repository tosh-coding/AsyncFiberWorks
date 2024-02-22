using AsyncFiberWorks.Channels;
using System;

namespace AsyncFiberWorks.Core
{
    ///<summary>
    /// Allows for the registration and deregistration of subscriptions
    ///</summary>
    public interface IDisposableSubscriptionRegistry : IDisposable
    {
        /// <summary>
        /// Create an unsubscriber who unsubscribes when the fiber is discarded.
        /// </summary>
        /// <returns>Unsubscriber with the unregister process as an element.</returns>
        Unsubscriber BeginSubscription();
    }
}