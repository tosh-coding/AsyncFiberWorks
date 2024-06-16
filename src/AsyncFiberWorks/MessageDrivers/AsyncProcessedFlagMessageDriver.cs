using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.MessageDrivers;

namespace AsyncFiberWorks.MessageFilters
{
    /// <summary>
    /// Deliver messages to all or some subscribers.
    /// Call all subscriber handlers in the order in which they were registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If it has not yet been processed, it proceeds to the next step. If already processed, exit at that point.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public class AsyncProcessedFlagMessageDriver<TMessage> : IAsyncMessageDriver<ProcessedFlagEventArgs<TMessage>>
    {
        private readonly AsyncActionList<ProcessedFlagEventArgs<TMessage>> _actions = new AsyncActionList<ProcessedFlagEventArgs<TMessage>>();
        private readonly IAsyncExecutor<ProcessedFlagEventArgs<TMessage>> _executorSingle;

        /// <summary>
        /// Create a driver with custom executors.
        /// </summary>
        /// <param name="executorSingle"></param>
        public AsyncProcessedFlagMessageDriver(IAsyncExecutor<ProcessedFlagEventArgs<TMessage>> executorSingle = null)
        {
            _executorSingle = executorSingle;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncProcessedFlagMessageDriver()
            : this(null)
        {
        }

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<ProcessedFlagEventArgs<TMessage>, Task> action)
        {
            return _actions.AddHandler(action);
        }

        /// <summary>
        /// Distribute one message.
        /// </summary>
        /// <param name="message">An message.</param>
        public async Task Invoke(ProcessedFlagEventArgs<TMessage> message)
        {
            await _actions.Invoke(message, Execute, _executorSingle).ConfigureAwait(false);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }

        /// <summary>
        /// Executes actions.
        /// If any action returns true, execution stops at that point.
        /// </summary>
        /// <param name="eventArgs">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <param name="executorSingle">Executor for one task. If null, the task is executed directly.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        async Task Execute(ProcessedFlagEventArgs<TMessage> eventArgs, IReadOnlyList<Func<ProcessedFlagEventArgs<TMessage>, Task>> actions, IAsyncExecutor<ProcessedFlagEventArgs<TMessage>> executorSingle)
        {
            if (actions == null)
            {
                return;
            }

            if (executorSingle != null)
            {
                foreach (var action in actions)
                {
                    await executorSingle.Execute(eventArgs, action).ConfigureAwait(false);
                    if (eventArgs.Processed)
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (var action in actions)
                {
                    await action(eventArgs).ConfigureAwait(false);
                    if (eventArgs.Processed)
                    {
                        break;
                    }
                }
            }
        }
    }
}
