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
        private readonly AsyncActionList<TMessage> _actions = new AsyncActionList<TMessage>();
        private readonly IAsyncExecutor<TMessage> _executorSingle;
        private List<Func<TMessage, Task>> _copied = new List<Func<TMessage, Task>>();

        /// <summary>
        /// Create a driver with custom executors.
        /// </summary>
        /// <param name="executorSingle"></param>
        public AsyncMessageDriver(IAsyncExecutor<TMessage> executorSingle)
        {
            _executorSingle = executorSingle;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncMessageDriver()
            : this(null)
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
        public async Task Invoke(TMessage message)
        {
            _actions.CopyTo(_copied);
            await Execute(message, _copied, _executorSingle).ConfigureAwait(false);
            _copied.Clear();
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="actions"></param>
        /// <param name="executorSingle"></param>
        /// <returns></returns>
        async Task Execute(TMessage arg, IReadOnlyList<Func<TMessage, Task>> actions, IAsyncExecutor<TMessage> executorSingle = null)
        {
            if (executorSingle == null)
            {
                foreach (var action in actions)
                {
                    await action.Invoke(arg).ConfigureAwait(false);
                }
            }
            else
            {
                foreach (var action in actions)
                {
                    await executorSingle.Execute(arg, action).ConfigureAwait(false);
                }
            }
        }
    }
}