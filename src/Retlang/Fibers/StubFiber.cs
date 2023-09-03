using System;
using System.Threading;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiber does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread.
    /// </summary>
    public class StubFiber : FiberWithDisposableList
    {
        private readonly StubFiberSlim _stubFiberSlim;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        public StubFiber()
            : this(new StubFiberSlim())
        {}

        /// <summary>
        /// Construct new instance.
        /// </summary>
        private StubFiber(StubFiberSlim stubFiberSlim)
            : base(stubFiberSlim)
        {
            _stubFiberSlim = stubFiberSlim;
        }

        /// <summary>
        /// No Op
        /// </summary>
        public override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// Number of pending actions.
        /// </summary>
        public int NumPendingActions
        {
            get { return _stubFiberSlim.NumPendingActions; }
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
        /// Execute actions until cancelled.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException">Cancelled.</exception>
        public void ExecuteUntilCancelled(CancellationToken cancellationToken)
        {
            _stubFiberSlim.ExecuteUntilCancelled(cancellationToken);
        }
    }
}
