using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default executor.
    /// </summary>
    public class DefaultAsyncExecutorSingle : IAsyncExecutorSingle
    {
        private bool _running = true;

        /// <summary>
        /// Executes a task. 
        /// </summary>
        /// <param name="func"></param>
        public async Task Execute(Func<Task> func)
        {
            if (_running)
            {
                await func.Invoke().ConfigureAwait(false);
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