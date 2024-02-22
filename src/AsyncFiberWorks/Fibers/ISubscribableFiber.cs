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
        /// Begin a subscription. Then set its unsubscriber to disposable.
        /// </summary>
        /// <param name="disposable">Disposables that can be reserved for unsubscriptions.</param>
        void BeginSubscriptionAndSetUnsubscriber(IDisposableSubscriptionRegistry disposable);
    }
}
