using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver subscription interface.
    /// </summary>
    public interface IAsyncActionSubscriber
    {
        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<Task> action);
    }
}
