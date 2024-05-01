using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes pending action(s).
    /// </summary>
    public interface IExecutor : IExecutorSingle
    {
        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="toExecute"></param>
        void Execute(IReadOnlyList<Action> toExecute);
    }
}