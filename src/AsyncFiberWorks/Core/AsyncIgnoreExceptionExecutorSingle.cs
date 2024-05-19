using AsyncFiberWorks.Procedures;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// An executor who ignores exceptions.
    /// </summary>
    public class AsyncIgnoreExceptionExecutorSingle : IAsyncExecutorSingle
    {
        private Action<Exception> _catchAction;

        /// <summary>
        /// Create an executor.
        /// </summary>
        /// <param name="catchAction">This is the process called when an exception is caught at executing.</param>
        public AsyncIgnoreExceptionExecutorSingle(Action<Exception> catchAction)
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
                await func();
            }
            catch (Exception ex)
            {
                _catchAction?.Invoke(ex);
            }
        }
    }
}
