using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Enqueue pending actions to the execution context.
    /// Subscription available for continued fiber use. All are cancelled when fiber is destroyed.
    /// It has a dedicated thread, and destruction of the fiber causes the thread to stop.
    /// </summary>
    public interface IThreadFiber : IFiber, IDisposable
    {
    }
}
