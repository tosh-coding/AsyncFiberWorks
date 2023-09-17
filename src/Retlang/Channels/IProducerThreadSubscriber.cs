using Retlang.Core;

namespace Retlang.Channels
{
    /// <summary>
    /// Action subscriber that receives actions on producer thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IProducerThreadSubscriber<T> : IProducerThreadSubscriberCore<T>
    {
        ///<summary>
        /// Allows for the registration and deregistration of subscriptions
        ///</summary>
        ISubscriptionRegistry Subscriptions { get; }
    }
}
