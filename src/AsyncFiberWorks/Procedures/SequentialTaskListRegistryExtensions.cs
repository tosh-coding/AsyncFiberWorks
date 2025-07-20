using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Extensions of task list registration interface.
    /// </summary>
    public static class SequentialTaskListRegistryExtensions
    {
        /// <summary>
        /// Add a task to the tail.
        /// </summary>
        /// <param name="task">Task to be performed.</param>
        /// <param name="context">The context in which the task will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public static IDisposable Add(this ISequentialTaskListRegistry me, Func<Task> task, IFiber context = null)
        {
            return me.Add((IFiberExecutionEventArgs e) => e.PauseWhileRunning(task), context);
        }
    }
}
