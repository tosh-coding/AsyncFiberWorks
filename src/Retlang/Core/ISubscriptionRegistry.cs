using System;

namespace Retlang.Core
{
    ///<summary>
    /// Allows for the registration and deregistration of subscriptions
    ///</summary>
    public interface ISubscriptionRegistry
    {
        ///<summary>
        /// Register subscription to be unsubcribed from when the fiber is disposed.
        ///</summary>
        ///<param name="toAdd"></param>
        void RegisterSubscription(IDisposable toAdd);

        /// <summary>
        /// Add Disposable. It will be unsubscribed when the fiber is discarded.
        /// It is destroyed at the last.
        /// </summary>
        /// <param name="toAdd"></param>
        void RegisterSubscriptionLast(IDisposable toAdd);

        ///<summary>
        /// Deregister a subscription.
        ///</summary>
        ///<param name="toRemove"></param>
        ///<returns></returns>
        bool DeregisterSubscription(IDisposable toRemove);

        /// <summary>
        /// Number of registered disposables.
        /// </summary>
        int NumSubscriptions { get; }
    }
}