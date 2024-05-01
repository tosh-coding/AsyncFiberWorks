using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver subscription interface.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    /// <typeparam name="TRet">Type of return value.</typeparam>
    public interface IAsyncActionSubscriber<TArg, TRet>
    {
        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<TArg, Task<TRet>> action);
    }
}
