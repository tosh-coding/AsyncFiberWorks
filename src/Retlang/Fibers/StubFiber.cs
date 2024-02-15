using Retlang.Channels;
using Retlang.Core;
using System;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiber does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread.
    /// </summary>
    public class StubFiber : IFiber, IConsumingContext
    {
        private readonly StubWorkerThread _stubFiberSlim;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a stub fiber with the default executor.
        /// </summary>
        public StubFiber()
        {
            _stubFiberSlim = new StubWorkerThread();
        }

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiber(IExecutor executor)
        {
            _stubFiberSlim = new StubWorkerThread(executor);
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }


        /// <summary>
        /// <see cref="ISubscriptionRegistry.CreateSubscription"/>
        /// </summary>
        public Unsubscriber CreateSubscription()
        {
            return _subscriptions.CreateSubscription();
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
            _stubFiberSlim.Queue((_) => action());
        }

        /// <summary>
        /// Execute all actions in the pending list.  If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        public int ExecuteAllPendingUntilEmpty()
        {
            return _stubFiberSlim.ExecuteAllPendingUntilEmpty();
        }

        /// <summary>
        /// Execute all actions in the pending list.
        /// </summary>
        public int ExecuteAllPending()
        {
            return _stubFiberSlim.ExecuteAllPending();
        }
    }
}
