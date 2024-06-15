using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Count the number of executions.
    /// </summary>
    public class CountExecutor : IExecutor
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
            action();
            Count += 1;
        }
    }
}
