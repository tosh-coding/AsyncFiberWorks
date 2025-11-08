using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// An interface for publishing messages sequentially.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public interface ISequentialPublisher<TMessage>
    {
        /// <summary>
        /// Publish one message to all registrants in sequence.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <returns>Tasks awaiting publication completion.</returns>
        Task PublishSequentialAsync(TMessage message);
    }
}
