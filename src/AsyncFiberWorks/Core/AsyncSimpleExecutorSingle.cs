using AsyncFiberWorks.Procedures;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Just simply execute.
    /// </summary>
    public class AsyncSimpleExecutorSingle : IAsyncExecutorSingle
    {
        /// <summary>
        /// Singleton instances.
        /// AsyncSimpleExecutorSingle has no members, so it can be shared.
        /// </summary>
        public static readonly AsyncSimpleExecutorSingle Instance = new AsyncSimpleExecutorSingle();

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task Execute(Func<Task> func)
        {
            await func();
        }
    }
}
