using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Action driver subscription interface.
    /// </summary>
    public interface IActionDriverSubscriber
    {
        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Action action);

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Action<FiberExecutionEventArgs> action);
    }
}
