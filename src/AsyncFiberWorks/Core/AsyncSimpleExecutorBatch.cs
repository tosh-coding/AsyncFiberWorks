using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Just simply execute actions.
    /// </summary>
    public class AsyncSimpleExecutorBatch : IAsyncExecutorBatch
    {
        /// <summary>
        /// Singleton instances.
        /// This class has no members, so it can be shared.
        /// </summary>
        public static readonly AsyncSimpleExecutorBatch Instance = new AsyncSimpleExecutorBatch();

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="actions"></param>
        public async Task Execute(IReadOnlyList<Func<Task>> actions)
        {
            foreach (var action in actions)
            {
                await action.Invoke().ConfigureAwait(false);
            }
        }
    }
}
