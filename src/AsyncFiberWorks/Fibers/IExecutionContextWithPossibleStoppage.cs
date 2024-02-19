using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Enqueue pending actions to the execution context.
    /// Can also register channel subscription status. Used to cancel them all at once when the fiber is destroyed.
    /// </summary>
    public interface IExecutionContextWithPossibleStoppage : ISubscriptionRegistry, IExecutionContext
    {
    }
}
