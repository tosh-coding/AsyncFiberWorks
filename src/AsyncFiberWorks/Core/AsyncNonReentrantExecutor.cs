using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// The same instance of this class will not be executed concurrently. The one executed later is skipped.
    /// </summary>
    public class AsyncNonReentrantExecutor : IAsyncExecutor
    {
        private readonly object _lockObj = new object();
        private bool _executing = false;

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task Execute(Func<Task> func)
        {
            lock (_lockObj)
            {
                if (_executing)
                {
                    return;
                }
                _executing = true;
            }

            try
            {
                await func().ConfigureAwait(false);
            }
            finally
            {
                lock (_lockObj)
                {
                    _executing = false;
                }
            }
        }
    }
}
