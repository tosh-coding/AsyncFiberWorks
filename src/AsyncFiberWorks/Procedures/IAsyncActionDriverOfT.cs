using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Drivers provide the timing of execution. It provides methods for invoking and subscribing to actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncActionDriver<T> : IAsyncActionSubscriber<T>, IAsyncActionInvoker<T>
    {
    }
}
