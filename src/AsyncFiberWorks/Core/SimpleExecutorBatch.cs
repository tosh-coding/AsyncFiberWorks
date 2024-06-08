using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Just simply execute actions.
    /// </summary>
    public class SimpleExecutorBatch : IExecutorBatch
    {
        /// <summary>
        /// Singleton instances.
        /// SimpleExecutor has no members, so it can be shared.
        /// </summary>
        public static readonly SimpleExecutorBatch Instance = new SimpleExecutorBatch();

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="toExecute"></param>
        /// <param name="executorSingle"></param>
        public void Execute(IReadOnlyList<Action> toExecute, IExecutor executorSingle)
        {
            foreach (var action in toExecute)
            {
                executorSingle.Execute(action);
            }
        }
    }
}
