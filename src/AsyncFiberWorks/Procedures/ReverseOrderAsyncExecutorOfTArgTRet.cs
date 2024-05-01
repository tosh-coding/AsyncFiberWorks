using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Call the actions in the reverse order in which they were registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If the return value is false, move on to the next action. If true, execution ends at that point.
    /// </summary>
    /// <typeparam name="TArg"></typeparam>
    public class ReverseOrderAsyncExecutorOfTArgTRet<TArg> : IAsyncExecutor<TArg, bool>
    {
        /// <summary>
        /// Call the actions in the reverse order in which they were registered.
        /// If any action returns true, execution stops at that point.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Execute(TArg arg, IReadOnlyList<Func<TArg, Task<bool>>> actions)
        {
            if (actions != null)
            {
                foreach (var a in actions.Reverse())
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
