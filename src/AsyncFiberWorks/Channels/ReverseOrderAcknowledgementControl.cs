using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Call the handlers in the reverse order in which they were registered.
    /// Wait for the calls to complete one at a time before proceeding.
    /// If ACK is false, it moves on to the next subscriber. If true, publish will be terminated at that point.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class ReverseOrderAcknowledgementControl<TMessage> : IAcknowledgementControl<TMessage, bool>
    {
        /// <summary>
        /// Publish a message and accept an ACK.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="handlers">A list of message recipients.</param>
        /// <returns>Wait for the publishing process to complete.</returns>
        public async Task OnPublish(TMessage msg, IReadOnlyList<Func<TMessage, Task<bool>>> handlers)
        {
            if (handlers != null)
            {
                foreach (var h in handlers.Reverse())
                {
                    bool ack = await h(msg).ConfigureAwait(false);
                    if (ack)
                    {
                        break;
                    }
                }
            }
        }
    }
}
