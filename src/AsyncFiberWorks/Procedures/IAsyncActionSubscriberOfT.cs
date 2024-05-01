using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver subscription interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncActionSubscriber<T>
    {
        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<T, Task> action);
    }
}
