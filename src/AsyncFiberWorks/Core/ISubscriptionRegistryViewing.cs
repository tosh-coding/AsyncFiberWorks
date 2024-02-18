namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Viewing information of subscriptions.
    /// </summary>
    public interface ISubscriptionRegistryViewing
    {
        /// <summary>
        /// Number of registered disposables.
        /// </summary>
        int NumSubscriptions { get; }
    }
}