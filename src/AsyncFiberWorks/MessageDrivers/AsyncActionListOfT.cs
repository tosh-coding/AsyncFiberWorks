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
        /// Copy all subscribers.
        /// </summary>
        /// <param name="destination"></param>
        public void CopyTo(List<Func<T, Task>> destination)
        {
            lock (_lock)
            {
                destination.AddRange(_actions);
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
