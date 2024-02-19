using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Enqueue pending actions to the execution context.
    /// Can also register channel subscription status. Used to cancel them all at once when the fiber is destroyed.
    /// It has a dedicated thread, and destruction of the fiber causes the thread to stop.
    /// </summary>
    public interface IThreadFiber : IFiber, IDisposable
    {
    }
}
