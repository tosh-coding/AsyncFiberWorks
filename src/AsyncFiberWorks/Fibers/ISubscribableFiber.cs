using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Enqueue pending actions to the execution context.
    /// Subscription available for continued fiber use. All are cancelled when fiber is destroyed.
    /// </summary>
    public interface ISubscribableFiber : IExecutionContext
    {
        /// <summary>
        /// Begin subscription.
        /// </summary>
        /// <returns>Unsubscribers. It is also discarded when the subscription subject is terminated.</returns>
        Unsubscriber BeginSubscription();
    }
}
