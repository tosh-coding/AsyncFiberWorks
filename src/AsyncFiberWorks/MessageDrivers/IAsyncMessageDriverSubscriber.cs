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
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<TMessage, Task> action);
    }
}
