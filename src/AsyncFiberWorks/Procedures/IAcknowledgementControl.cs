using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Control message transmission.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    public interface IAcknowledgementControl<TMessage, TAck>
    {
        /// <summary>
        /// Handle the response from the notification destination.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="handlers">A list of message recipients.</param>
        /// <returns>Wait for the publishing process to complete.</returns>
        Task OnPublish(TMessage msg, IReadOnlyList<Func<TMessage, Task<TAck>>> handlers);
    }
}
