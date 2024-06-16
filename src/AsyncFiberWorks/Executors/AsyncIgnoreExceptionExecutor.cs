using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Executors
{
    /// <summary>
    /// An executor who ignores exceptions.
    /// </summary>
    public class AsyncIgnoreExceptionExecutor : IAsyncExecutor
    {
        private Action<Exception> _catchAction;

        /// <summary>
        /// Create an executor.
        /// </summary>
        /// <param name="catchAction">This is the process called when an exception is caught at executing.</param>
        public AsyncIgnoreExceptionExecutor(Action<Exception> catchAction)
        {
            _catchAction = catchAction;
        }

        /// <summary>
        /// Start a task and wait for it to finish.
        /// Exceptions that occur during this time are ignored.
        /// </summary>
        /// <param name="func">Target task.</param>
        /// <returns></returns>
        public async Task Execute(Func<Task> func)
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _catchAction?.Invoke(ex);
            }
        }
    }
}
