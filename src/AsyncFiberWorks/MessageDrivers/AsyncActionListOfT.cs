using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.MessageDrivers
{
    /// <summary>
    /// List of actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class AsyncActionList<T>
    {
        private object _lock = new object();
        private LinkedList<Func<T, Task>> _actions = new LinkedList<Func<T, Task>>();
        private List<Func<T, Task>> _copied = new List<Func<T, Task>>();
        private bool _publishing;

        /// <summary>
        /// Add an action.
        /// </summary>
        /// <param name="action">An action.</param>
        /// <returns>Function for removing the action.</returns>
        public IDisposable AddHandler(Func<T, Task> action)
        {
            var maskableFilter = new ToggleFilter();
            Func<T, Task> safeAction = async (message) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    await action(message);
                }
            };

            lock (_lock)
            {
                _actions.AddLast(safeAction);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                lock (_lock)
                {
                    maskableFilter.IsEnabled = false;
                    _actions.Remove(safeAction);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="executorBatch"></param>
        /// <param name="executorSingle"></param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Invoke(T arg, Func<T, IReadOnlyList<Func<T, Task>>, IAsyncExecutor<T>, Task> executorBatch, IAsyncExecutor<T> executorSingle)
        {
            lock (_lock)
            {
                if (_publishing)
                {
                    throw new InvalidOperationException("Cannot be executed in parallel.");
                }
                _publishing = true;
                _copied.Clear();
                _copied.AddRange(_actions);
            }
            try
            {
                await executorBatch(arg, _copied, executorSingle).ConfigureAwait(false);
            }
            finally
            {
                lock (_lock)
                {
                    _publishing = false;
                }
            }
        }

        /// <summary>
        /// Number of actions.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _actions.Count;
                }
            }
        }
    }
}
