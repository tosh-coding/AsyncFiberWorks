using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Acknowledgement channel subscription interface.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    public interface IAcknowledgementSubscriber<TMessage, TAck>
    {
        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="messageReceiver">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(Func<TMessage, Task<TAck>> messageReceiver);
    }
}
