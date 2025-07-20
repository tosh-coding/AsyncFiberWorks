using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Task list registration interface.
    /// </summary>
    public interface ISequentialTaskListRegistry
    {
        /// <summary>
        /// Add an action to the tail.
        /// </summary>
        /// <param name="action">Action to be performed.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        IDisposable Add(Action action, IFiber context = null);

        /// <summary>
        /// Add a task to the tail.
        /// </summary>
        /// <param name="task">Task to be performed.</param>
        /// <param name="context">The context in which the task will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        IDisposable Add(Action<IFiberExecutionEventArgs> task, IFiber context = null);
    }
}
