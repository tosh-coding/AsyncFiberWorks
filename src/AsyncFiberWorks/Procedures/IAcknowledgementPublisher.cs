using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Sender interface to channels with acknowledgments.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    public interface IAcknowledgementPublisher<TMessage, TAck>
    {
        /// <summary>
        /// Publish a message to subscribers.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="control">Publishing controller.</param>
        /// <returns>Waiting for the publishing process to complete.</returns>
        Task Publish(TMessage msg, IAcknowledgementControl<TMessage, TAck> control);
    }
}
