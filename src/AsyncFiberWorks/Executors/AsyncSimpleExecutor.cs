using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Executors
{
    /// <summary>
    /// Just simply execute.
    /// </summary>
    public class AsyncSimpleExecutor : IAsyncExecutor
    {
        /// <summary>
        /// Singleton instances.
        /// This class has no members, so it can be shared.
        /// </summary>
        public static readonly AsyncSimpleExecutor Instance = new AsyncSimpleExecutor();

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task Execute(Func<Task> func)
        {
            await func().ConfigureAwait(false);
        }
    }
}
