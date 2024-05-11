using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Enqueue pending actions to the execution context.
    /// Subscription available for continued fiber use. All are cancelled when fiber is destroyed.
    /// </summary>
    public interface IFiber : IExecutionContext, IDisposable
    {
    }
}
