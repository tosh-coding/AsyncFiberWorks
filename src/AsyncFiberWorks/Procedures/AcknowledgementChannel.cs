using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// This channel is a publishing interface controlled by an acknowledgment.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    public class AcknowledgementChannel<TMessage, TAck> : IAcknowledgementChannel<TMessage, TAck>
    {
        private readonly AcknowledgementMessageHandlerList<TMessage, TAck> _channel = new AcknowledgementMessageHandlerList<TMessage, TAck>();

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="messageReceiver">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<TMessage, Task<TAck>> messageReceiver)
        {
            return _channel.AddHandler(messageReceiver);
        }

        /// <summary>
        /// Publish a message to subscribers.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="control">Publishing controller.</param>
        /// <returns>Waiting for the publishing process to complete.</returns>
        public async Task Publish(TMessage msg, IAcknowledgementControl<TMessage, TAck> control)
        {
            await _channel.Publish(msg, control);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _channel.Count; } }
    }
}
