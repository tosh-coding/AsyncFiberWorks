using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// An executor that can be toggled to run or skip.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncMaskableExecutor<T> : IAsyncExecutor<T>
    {
        private bool _running = true;

        /// <summary>
        /// Executes a single action. 
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="action"></param>
        public async Task Execute(T arg, Func<T, Task> action)
        {
            if (_running)
            {
                await action.Invoke(arg).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// When disabled, actions will be ignored by executor. The executor is typically disabled at shutdown
        /// to prevent any pending actions from being executed. 
        /// </summary>
        public bool IsEnabled
        {
            get { return _running; }
            set { _running = value; }
        }
    }
}
