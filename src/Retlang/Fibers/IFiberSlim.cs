using System;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Enqueues pending actions for the context of execution (thread, pool of threads, message pump, etc.)
    /// </summary>
    public interface IFiberSlim : IExecutionContext, IDisposable
    {
        /// <summary>
        /// Start consuming actions.
        /// </summary>
        void Start();
    }
}
