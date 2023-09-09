using System;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Enqueues pending actions for the context of execution (thread, pool of threads, message pump, etc.)
    /// Subscription and scheduling functions have been added.
    /// Mainly for channels.
    /// </summary>
    public interface IFiber : ISubscriptionRegistry, IExecutionContext, IScheduler, IDisposable
    {
        /// <summary>
        /// Start consuming actions.
        /// </summary>
        void Start();
    }
}
