using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes pending actions.
    /// </summary>
    public interface IExecutorBatch
    {
        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="actions">A list of actions.</param>
        /// <param name="executorSingle">The executor for each operation.</param>
        void Execute(IReadOnlyList<Action> actions, IExecutor executorSingle);
    }
}