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
        /// <param name="toExecute"></param>
        /// <param name="executorSingle"></param>
        void Execute(IReadOnlyList<Action> toExecute, IExecutor executorSingle);
    }
}