using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default executor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultAsyncExecutor<T> : IAsyncExecutor<T>
    {
        private bool _running = true;

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public async Task Execute(T arg, IReadOnlyList<Func<T, Task>> actions)
        {
            foreach (var action in actions)
            {
                await Execute(arg, action);
            }
        }

        /// <summary>
        /// Executes a single action. 
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="action"></param>
        public async Task Execute(T arg, Func<T, Task> action)
        {
            if (_running)
            {
                await action.Invoke(arg);
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