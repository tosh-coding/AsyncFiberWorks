using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Executes actions.
    /// </summary>
    public interface IAsyncExecutor
    {
        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="actions">A list of actions.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        Task Execute(IReadOnlyList<Func<Task>> actions);
    }
}
