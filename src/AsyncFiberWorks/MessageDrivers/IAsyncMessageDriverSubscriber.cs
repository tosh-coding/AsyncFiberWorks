using AsyncFiberWorks.Fibers;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.MessageDrivers
{
    /// <summary>
    /// Message driver subscription interface.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public interface IAsyncMessageDriverSubscriber<TMessage>
    {
        /// <summary>
        /// Subscribe a message driver.
        /// </summary>
        /// <param name="action">Message receiver.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Action<TMessage> action, IFiber context = null);

        /// <summary>
        /// Subscribe a message driver.
        /// </summary>
        /// <param name="action">Message receiver.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<TMessage, Task> action, IFiber context = null);
    }
}
