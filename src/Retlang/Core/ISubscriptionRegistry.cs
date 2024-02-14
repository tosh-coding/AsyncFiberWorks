using Retlang.Channels;
using System;

namespace Retlang.Core
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
        Unsubscriber CreateUnsubscriber();

        /// <summary>
        /// Number of registered disposables.
        /// </summary>
        int NumSubscriptions { get; }
    }
}