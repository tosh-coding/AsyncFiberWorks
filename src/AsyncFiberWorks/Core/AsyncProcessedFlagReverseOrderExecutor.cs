using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Call the actions in the reverse order in which they were registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If unprocessed, proceeds to the next action. If already processed, execution ends at that point.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncProcessedFlagReverseOrderExecutor<T> : IAsyncExecutorBatch<ProcessedFlagEventArgs<T>>
    {
        /// <summary>
        /// Call the actions in the reverse order in which they were registered.
        /// If any action is processed, execution stops at that point.
        /// </summary>
        /// <param name="eventArgs">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <param name="executorSingle">Executor for one task. If null, the task is executed directly.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Execute(ProcessedFlagEventArgs<T> eventArgs, IReadOnlyList<Func<ProcessedFlagEventArgs<T>, Task>> actions, IAsyncExecutor<ProcessedFlagEventArgs<T>> executorSingle)
        {
            if (actions == null)
            {
                return;
            }

            if (executorSingle != null)
            {
                foreach (var action in actions.Reverse())
                {
                    await executorSingle.Execute(eventArgs, action).ConfigureAwait(false);
                    if (eventArgs.Processed)
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (var action in actions.Reverse())
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
