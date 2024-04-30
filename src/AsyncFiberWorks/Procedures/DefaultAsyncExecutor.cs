using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default executor.
    /// </summary>
    public class DefaultAsyncExecutor : IAsyncExecutor
    {
        private bool _running = true;

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="actions"></param>
        public async Task Execute(IReadOnlyList<Func<Task>> actions)
        {
            foreach (var action in actions)
            {
                await Execute(action);
            }
        }

        ///<summary>
        /// Executes a single action. 
        ///</summary>
        ///<param name="action"></param>
        public async Task Execute(Func<Task> action)
        {
            if (_running)
            {
                await action.Invoke();
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