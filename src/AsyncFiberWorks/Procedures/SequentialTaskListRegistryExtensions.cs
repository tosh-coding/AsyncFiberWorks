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

        /// <summary>
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler. The return value is the processed flag. If this flag is true, subsequent handlers are not called.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public static IDisposable Add<TMessage>(this ISequentialHandlerListRegistry<TMessage> me, Func<TMessage, Task<bool>> handler, IFiber context = null)
        {
            return me.Add((IFiberExecutionEventArgs e, ProcessedFlagEventArgs<TMessage> message) => e.PauseWhileRunning(async () =>
            {
                message.Processed = await handler(message.Arg).ConfigureAwait(false);
            }), context);
        }
    }
}
