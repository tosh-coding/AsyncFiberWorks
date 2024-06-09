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
        void Execute(IReadOnlyList<Action> actions);
    }
}