using AsyncFiberWorks.Core;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Channel subscription interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriber<T>
    {
        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(IExecutionContext executionContext, Action<T> receive);

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(IAsyncExecutionContext executionContext, Func<T, Task<Action>> receive);
    }
}
