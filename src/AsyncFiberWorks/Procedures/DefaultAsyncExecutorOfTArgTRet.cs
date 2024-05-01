using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default executor.
    /// Call all actions in the order in which they are registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If the return value is false, move on to the next action. If true, execution ends at that point.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    public class DefaultAsyncExecutorOfTArgTRet<TArg> : IAsyncExecutor<TArg, bool>
    {
        /// <summary>
        /// Executes actions.
        /// If any action returns true, execution stops at that point.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Execute(TArg arg, IReadOnlyList<Func<TArg, Task<bool>>> actions)
        {
            if (actions != null)
            {
                foreach (var a in actions)
                {
                    bool ack = await a(arg).ConfigureAwait(false);
                    if (ack)
                    {
                        break;
                    }
                }
            }
        }
    }
}
