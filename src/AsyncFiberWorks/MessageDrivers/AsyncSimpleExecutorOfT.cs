using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.MessageFilters
{
    /// <summary>
    /// Just simply execute.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncSimpleExecutor<T> : IAsyncExecutor<T>
    {
        /// <summary>
        /// Singleton instances.
        /// This class has no members, so it can be shared.
        /// </summary>
        public static readonly AsyncSimpleExecutor<T> Instance = new AsyncSimpleExecutor<T>();

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task Execute(T arg, Func<T, Task> func)
        {
            await func(arg).ConfigureAwait(false);
        }
    }
}
