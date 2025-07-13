using AsyncFiberWorks.Fibers;
using System;
using System.Threading.Tasks;

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
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Action action, IFiber context = null);

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<Task> action, IFiber context = null);
    }
}
