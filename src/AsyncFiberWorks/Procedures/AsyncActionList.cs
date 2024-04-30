using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Channels;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of actions.
    /// </summary>
    internal sealed class AsyncActionList
    {
        private object _lock = new object();
        private LinkedList<Func<Task>> _actions = new LinkedList<Func<Task>>();
        private List<Func<Task>> _copied = new List<Func<Task>>();
        private bool _publishing;

        /// <summary>
        /// Add an action.
        /// </summary>
        /// <param name="action">An action.</param>
        /// <returns>Function for removing the action.</returns>
        public IDisposable AddHandler(Func<Task> action)
        {
            lock (_lock)
            {
                _actions.AddLast(action);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                lock (_lock)
                {
                    _actions.Remove(action);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="executor"></param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Invoke(IAsyncExecutor executor)
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
                await executor.Execute(_copied).ConfigureAwait(false);
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
