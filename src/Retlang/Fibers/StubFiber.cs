using System;
using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiber does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread.
    /// </summary>
    public class StubFiber : FiberWithDisposableList, IConsumingContext
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
        /// Create a new instance and call the Start method.
        /// </summary>
        /// <returns></returns>
        public static StubFiber StartNew()
        {
            var fiber = new StubFiber();
            fiber.Start();
            return fiber;
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
