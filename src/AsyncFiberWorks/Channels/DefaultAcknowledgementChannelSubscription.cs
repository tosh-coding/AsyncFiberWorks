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
        private readonly IMessageFilter<TMessage> _filter;

        /// <summary>
        /// Construct the subscription
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="filter"></param>
        public DefaultAcknowledgementChannelSubscription(Func<TMessage, Task<bool>> receiver, IMessageFilter<TMessage> filter = null)
        {
            _receiver = receiver;
            _filter = filter;
        }

        /// <summary>
        /// Message receive handler with acknowledgement.
        /// </summary>
        /// <param name="msg">A received message.</param>
        /// <returns>True if accepted, false if ignored.</returns>
        public async Task<bool> OnReceive(TMessage msg)
        {
            if (_filter?.PassesProducerThreadFilter(msg) ?? true)
            {
                return await _receiver(msg);
            }
            return false;
        }
    }
}