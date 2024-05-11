using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Enqueue pending actions to the execution context.
    /// </summary>
    public interface IFiber : IExecutionContext
    {
    }
}
