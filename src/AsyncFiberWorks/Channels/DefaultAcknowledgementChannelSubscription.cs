using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscription for actions on a acknowledgement channel.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class DefaultAcknowledgementChannelSubscription<TMessage> : IAcknowledgeMessageReceiver<TMessage, bool>
    {
        private readonly Func<TMessage, Task<bool>> _receiver;

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="receiver"></param>
        public DefaultAcknowledgementChannelSubscription(Func<TMessage, Task<bool>> receiver)
        {
            _receiver = receiver;
        }

        /// <summary>
        /// Message receive handler with acknowledgement.
        /// </summary>
        /// <param name="msg">A received message.</param>
        /// <returns>True if accepted, false if ignored.</returns>
        public async Task<bool> OnReceive(TMessage msg)
        {
            return await _receiver(msg);
        }
    }
}