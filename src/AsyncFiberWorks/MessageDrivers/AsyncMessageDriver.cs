using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly AsyncActionList<TMessage> _actions = new AsyncActionList<TMessage>();
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
            return _actions.AddHandler(action);
        }

        /// <summary>
        /// Distribute one message.
        /// </summary>
        /// <param name="message">An message.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task InvokeAsync(TMessage message)
        {
            _actions.CopyTo(_copied);
            foreach (var action in _copied)
            {
                await action(message).ConfigureAwait(false);
            }
            _copied.Clear();
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}