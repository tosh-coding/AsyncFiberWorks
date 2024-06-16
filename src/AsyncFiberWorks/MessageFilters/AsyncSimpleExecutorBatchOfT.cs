using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.MessageFilters
{
    /// <summary>
    /// Just simply execute actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncSimpleExecutorBatch<T> : IAsyncExecutorBatch<T>
    {
        /// <summary>
        /// Singleton instances.
        /// This class has no members, so it can be shared.
        /// </summary>
        public static readonly AsyncSimpleExecutorBatch<T> Instance = new AsyncSimpleExecutorBatch<T>();

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="actions"></param>
        /// <param name="executorSingle"></param>
        /// <returns></returns>
        public async Task Execute(T arg, IReadOnlyList<Func<T, Task>> actions, IAsyncExecutor<T> executorSingle = null)
        {
            if (executorSingle == null)
            {
                foreach (var action in actions)
                {
                    await action.Invoke(arg).ConfigureAwait(false);
                }
            }
            else
            {
                foreach (var action in actions)
                {
                    await executorSingle.Execute(arg, action).ConfigureAwait(false);
                }
            }
        }
    }
}