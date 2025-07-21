using AsyncFiberWorks.Core;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Timers
{
    /// <summary>
    /// The same instance of this class will not be executed concurrently.
    /// The one executed later is skipped.
    /// </summary>
    public class AsyncNonReentrantFiberScheduler
    {
        private readonly object _lockObj = new object();
        private readonly IAsyncExecutionContext _fiber;
        private bool _executing = false;

        /// <summary>
        /// Create a scheduler.
        /// </summary>
        /// <param name="fiber"></param>
        public AsyncNonReentrantFiberScheduler(IAsyncExecutionContext fiber)
        {
            _fiber = fiber;
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        public void Schedule(Func<Task> func)
        {
            lock (_lockObj)
            {
                if (_executing)
                {
                    return;
                }
                _executing = true;
            }

            _fiber.EnqueueTask(async () =>
            {
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
            });
        }
    }
}
