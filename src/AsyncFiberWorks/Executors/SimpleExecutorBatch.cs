using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Executors
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
        public void Execute(IReadOnlyList<Action> toExecute)
        {
            foreach (var action in toExecute)
            {
                action();
            }
        }
    }
}
