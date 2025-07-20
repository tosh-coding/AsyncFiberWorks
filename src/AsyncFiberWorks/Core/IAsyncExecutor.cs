using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes a task.
    /// </summary>
    public interface IAsyncExecutor
    {
        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="e">Fiber pause operation interface.</param>
        /// <param name="action">Action. Support pause.</param>
        void Execute(IFiberExecutionEventArgs e, Action<IFiberExecutionEventArgs> action);
    }
}
