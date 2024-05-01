using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Drivers provide the timing of execution. It provides methods for invoking and subscribing to actions.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    /// <typeparam name="TRet">Type of return value.</typeparam>
    public interface IAsyncActionDriver<TArg, TRet> : IAsyncActionSubscriber<TArg, TRet>, IAsyncActionInvoker<TArg, TRet>
    {
    }
}
