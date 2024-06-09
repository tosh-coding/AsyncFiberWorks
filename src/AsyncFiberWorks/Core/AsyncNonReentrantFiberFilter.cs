using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// The same instance of this class will not be executed concurrently.
    /// The one executed later is skipped.
    /// </summary>
    public class AsyncNonReentrantFiberFilter : IAsyncFiber
    {
        private readonly object _lockObj = new object();
        private readonly IAsyncFiber _fiber;
        private bool _executing = false;

        /// <summary>
        /// Create a filter.
        /// </summary>
        /// <param name="fiber"></param>
        public AsyncNonReentrantFiberFilter(IAsyncFiber fiber)
        {
            _fiber = fiber;
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        public void Enqueue(Func<Task> func)
        {
            lock (_lockObj)
            {
                if (_executing)
                {
                    return;
                }
                _executing = true;
            }

            _fiber.Enqueue(async () =>
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
