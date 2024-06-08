using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncExecutorBatch<T>
    {
        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <param name="executorSingle">The executor for each operation.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        Task Execute(T arg, IReadOnlyList<Func<T, Task>> actions, IAsyncExecutor<T> executorSingle);
    }
}
