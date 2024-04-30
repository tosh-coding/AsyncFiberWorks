using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Drivers provide the timing of execution. It provides methods for invoking and subscribing to actions.
    /// The class is thread safe.
    /// </summary>
    public interface IAsyncActionDriver : IAsyncActionSubscriber, IAsyncActionInvoker
    {
    }
}
