using System;
using System.Collections.Generic;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiber does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread. This class
    /// is not thread safe and should not be used in production code. 
    /// 
    /// The class is typically used for testing asynchronous code to make it completely synchronous and
    /// deterministic.
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
        /// All pending actions.
        /// </summary>
        public List<Action> Pending
        {
            get { return _stubFiberSlim.Pending; }
        }

        /// <summary>
        /// If true events will be executed immediately rather than added to the pending list.
        /// </summary>
        public bool ExecutePendingImmediately {
            get { return _stubFiberSlim.ExecutePendingImmediately; }
            set { _stubFiberSlim.ExecutePendingImmediately = value; } }

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
    }
}
