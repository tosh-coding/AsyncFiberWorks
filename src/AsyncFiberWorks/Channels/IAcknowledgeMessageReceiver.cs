using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Message receive handler with acknowledgement.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    public interface IAcknowledgeMessageReceiver<TMessage, TAck>
    {
        /// <summary>
        /// Message receive handler with acknowledgement.
        /// </summary>
        /// <param name="msg">A received message.</param>
        /// <returns>Tasks waiting for subscribers to be received.</returns>
        Task<TAck> OnReceive(TMessage msg);
    }
}
