using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// This is a fiber that needs to be pumped manually.
    /// Queued actions are added to the pending list.
    /// Consume them by periodically calling methods for execution.
    /// Periodically call a method for execution. They are executed on their calling thread.
    /// </summary>
    public class StubFiber : IFiber
    {
        private readonly StubFiberSlim _stubFiberSlim;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a stub fiber with the default executor.
        /// </summary>
        public StubFiber()
        {
            _stubFiberSlim = new StubFiberSlim();
        }

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiber(IExecutor executor)
        {
            _stubFiberSlim = new StubFiberSlim(executor);
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        /// <summary>
        /// <see cref="Subscriptions.BeginSubscription"/>
        /// </summary>
        /// <returns></returns>
        public Unsubscriber BeginSubscription()
        {
            return _subscriptions.BeginSubscription();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistry.NumSubscriptions"/>
        /// </summary>
        public int NumSubscriptions
        {
            get { return _subscriptions.NumSubscriptions; }
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _stubFiberSlim.Enqueue(action);
        }

        /// <summary>
        /// Execute until there are no more pending actions.
        /// </summary>
        public int ExecuteAll()
        {
            return _stubFiberSlim.ExecuteAll();
        }

        /// <summary>
        /// Execute only what is pending now.
        /// </summary>
        public int ExecuteOnlyPendingNow()
        {
            return _stubFiberSlim.ExecuteOnlyPendingNow();
        }
    }
}
