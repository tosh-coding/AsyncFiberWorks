using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver subscription interface.
    /// </summary>
    public interface IActionSubscriber
    {
        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Action action);
    }
}
