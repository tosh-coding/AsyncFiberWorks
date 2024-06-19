using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.MessageDrivers;

namespace AsyncFiberWorks.MessageFilters
{
    /// <summary>
    /// Distribute the message to all subscribers.
    /// Call all subscriber handlers in the order in which they were registered.
    /// Wait for the calls to complete one by one before proceeding.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public class AsyncMessageDriver<TMessage> : IAsyncMessageDriver<TMessage>
    {
        private object _lock = new object();
        private LinkedList<Func<TMessage, Task>> _actions = new LinkedList<Func<TMessage, Task>>();
        private List<Func<TMessage, Task>> _copied = new List<Func<TMessage, Task>>();

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncMessageDriver()
        {
        }

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<TMessage, Task> action)
        {
            var maskableFilter = new ToggleFilter();
            Func<TMessage, Task> safeAction = async (message) =>
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
        /// Distribute one message.
        /// </summary>
        /// <param name="message">An message.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task InvokeAsync(TMessage message)
        {
            lock (_lock)
            {
                _copied.AddRange(_actions);
            }
            foreach (var action in _copied)
            {
                await action(message).ConfigureAwait(false);
            }
            _copied.Clear();
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers
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