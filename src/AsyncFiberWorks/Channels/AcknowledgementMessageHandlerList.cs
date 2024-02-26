using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// List of message handlers with acknowledgement.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    internal sealed class AcknowledgementMessageHandlerList<TMessage, TAck>
    {
        private object _lock = new object();
        private LinkedList<Func<TMessage, Task<TAck>>> _handlers = new LinkedList<Func<TMessage, Task<TAck>>>();

        /// <summary>
        /// Add a message handler.
        /// </summary>
        /// <param name="action">A message handler.</param>
        /// <returns>Function for removing the handler.</returns>
        public IDisposable AddHandler(Func<TMessage, Task<TAck>> action)
        {
            _handlers.AddLast(action);

            var unsubscriber = new Unsubscriber(() => {
                lock (_lock)
                {
                    this._handlers.Remove(action);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Send a message to receive handlers.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="control">Publishing controller.</param>
        /// <returns>A task that waits for IAcknowledgeControl.OnPublish to complete.</returns>
        public async Task Publish(TMessage msg, IAcknowledgementControl<TMessage, TAck> control)
        {
            Func<TMessage, Task<TAck>>[] copied;
            lock (_lock)
            {
                copied = _handlers.ToArray();
            }
            await control.OnPublish(msg, copied).ConfigureAwait(false);
        }

        /// <summary>
        /// Number of handlers.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _handlers.Count;
                }
            }
        }
    }
}
