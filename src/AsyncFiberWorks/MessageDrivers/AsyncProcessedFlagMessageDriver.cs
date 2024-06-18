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
        private List<Func<ProcessedFlagEventArgs<TMessage>, Task>> _copied = new List<Func<ProcessedFlagEventArgs<TMessage>, Task>>();

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
            : this(new AsyncSimpleExecutor<ProcessedFlagEventArgs<TMessage>>())
        {
        }

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<ProcessedFlagEventArgs<TMessage>, Task> action)
        {
            return _actions.AddHandler((e) => _executorSingle.Execute(e, action));
        }

        /// <summary>
        /// Distribute one message.
        /// </summary>
        /// <param name="message">An message.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task InvokeAsync(ProcessedFlagEventArgs<TMessage> message)
        {
            _actions.CopyTo(_copied);
            foreach (var action in _copied)
            {
                await action(message).ConfigureAwait(false);

                // If any action returns true, execution stops at that point.
                if (message.Processed)
                {
                    break;
                }
            }
            _copied.Clear();
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
