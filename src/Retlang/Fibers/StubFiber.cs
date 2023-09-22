using System;
using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiber does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread.
    /// </summary>
    public class StubFiber : IFiber, IConsumingContext
    {
        private readonly StubFiberSlim _stubFiberSlim;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a stub fiber with the default executor.
        /// </summary>
        public StubFiber()
            : this(new StubFiberSlim())
        {}

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiber(IExecutor executor)
            : this(new StubFiberSlim(executor))
        {}

        /// <summary>
        /// Construct new instance.
        /// </summary>
        private StubFiber(StubFiberSlim stubFiberSlim)
        {
            _stubFiberSlim = stubFiberSlim;
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        /// <summary>
        /// <see cref="IExecutionContext.Enqueue(Action)"/>
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _stubFiberSlim.Enqueue(action);
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistryGetter.FallbackDisposer"/>
        /// </summary>
        public ISubscriptionRegistry FallbackDisposer
        {
            get { return _subscriptions; }
        }

        /// <summary>
        /// Execute all actions in the pending list.  If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        public void ExecuteAllPendingUntilEmpty()
        {
            _stubFiberSlim.ExecuteAllPendingUntilEmpty();
        }

        /// <summary>
        /// Execute all actions in the pending list.
        /// </summary>
        public void ExecuteAllPending()
        {
            _stubFiberSlim.ExecuteAllPending();
        }

        /// <summary>
        /// Execute actions until canceled.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException">Canceled.</exception>
        public void ExecuteUntilCanceled(CancellationToken cancellationToken)
        {
            _stubFiberSlim.ExecuteUntilCanceled(cancellationToken);
        }
    }
}
