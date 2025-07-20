using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Executors
{
    /// <summary>
    /// Count the number of executions.
    /// </summary>
    public class CountExecutor : IActionExecutor
    {
        /// <summary>
        /// Number of executions.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Execute a single action. 
        /// </summary>
        /// <param name="action"></param>
        public void Execute(Action action)
        {
            Count += 1;
            action();
        }

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="e">Fiber pause operation interface.</param>
        /// <param name="action">Action. Support pause.</param>
        public void Execute(IFiberExecutionEventArgs e, Action<IFiberExecutionEventArgs> action)
        {
            Count += 1;
            action(e);
        }
    }
}
