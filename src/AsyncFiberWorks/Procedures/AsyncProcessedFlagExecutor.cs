using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Call all actions in the order in which they are registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If unprocessed, proceeds to the next action. If already processed, execution ends at that point.
    /// </summary>
    /// <typeparam name="T">Type of argument.</typeparam>
    public class AsyncProcessedFlagExecutor<T> : IAsyncExecutor<ProcessedFlagEventArgs<T>>
    {
        /// <summary>
        /// Executes actions.
        /// If any action returns true, execution stops at that point.
        /// </summary>
        /// <param name="eventArgs">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Execute(ProcessedFlagEventArgs<T> eventArgs, IReadOnlyList<Func<ProcessedFlagEventArgs<T>, Task>> actions)
        {
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    await action(eventArgs).ConfigureAwait(false);
                    if (eventArgs.Processed)
                    {
                        break;
                    }
                }
            }
        }
    }
}
