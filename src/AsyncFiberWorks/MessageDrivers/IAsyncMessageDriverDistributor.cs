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
        /// <returns>Tasks waiting for call completion.</returns>
        Task Invoke(TMessage message);
    }
}
