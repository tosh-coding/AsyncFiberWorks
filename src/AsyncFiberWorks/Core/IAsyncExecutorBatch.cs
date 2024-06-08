using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes actions.
    /// </summary>
    public interface IAsyncExecutorBatch
    {
        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="actions">A list of actions.</param>
        /// <param name="executorSingle">The executor for each operation.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        Task Execute(IReadOnlyList<Func<Task>> actions, IAsyncExecutor executorSingle);
    }
}
