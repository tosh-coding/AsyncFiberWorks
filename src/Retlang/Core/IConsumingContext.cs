using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// Context for consuming a stub fiber queue.
    /// </summary>
    internal interface IConsumingContext
    {
        /// <summary>
        /// Execute all actions in the pending list.  If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        void ExecuteAllPendingUntilEmpty();

        /// <summary>
        /// Execute all actions in the pending list.
        /// </summary>
        void ExecuteAllPending();
    }
}
