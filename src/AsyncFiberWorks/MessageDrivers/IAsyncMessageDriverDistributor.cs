using AsyncFiberWorks.Fibers;
using System.Threading.Tasks;

namespace AsyncFiberWorks.MessageDrivers
{
    /// <summary>
    /// Message driver distribution interface.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public interface IAsyncMessageDriverDistributor<TMessage>
    {
        /// <summary>
        /// Distribute one message.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        Task InvokeAsync(TMessage message, IFiber defaultContext);
    }
}
