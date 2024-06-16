using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.MessageDrivers;

namespace AsyncFiberWorks.MessageFilters
{
    /// <summary>
    /// Deliver messages to all or some subscribers.
    /// Call the subscriber's message handlers in the reverse order in which they were registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If the result is unprocessed, proceed to the next step. If it has already been processed, exit at that point.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public class AsyncProcessedFlagReverseOrderMessageDriver<TMessage> : IAsyncMessageDriver<ProcessedFlagEventArgs<TMessage>>
    {
        private readonly AsyncActionList<ProcessedFlagEventArgs<TMessage>> _actions = new AsyncActionList<ProcessedFlagEventArgs<TMessage>>();
        private readonly IAsyncExecutor<ProcessedFlagEventArgs<TMessage>> _executorSingle;

        /// <summary>
        /// Create a driver with custom executors.
        /// </summary>
        /// <param name="executorSingle"></param>
        public AsyncProcessedFlagReverseOrderMessageDriver(IAsyncExecutor<ProcessedFlagEventArgs<TMessage>> executorSingle = null)
        {
            _executorSingle = executorSingle;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncProcessedFlagReverseOrderMessageDriver()
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
        /// Call the actions in the reverse order in which they were registered.
        /// If any action is processed, execution stops at that point.
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
                foreach (var action in actions.Reverse())
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
                foreach (var action in actions.Reverse())
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
