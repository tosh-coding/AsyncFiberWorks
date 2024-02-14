using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Enqueues pending actions for the context of execution (thread, pool of threads, message pump, etc.)
    /// Can also register channel subscription status. Used to cancel them all at once when the fiber is destroyed.
    /// </summary>
    public interface IExecutionContextWithPossibleStoppage : ISubscriptionRegistry, IExecutionContext
    {
    }
}
