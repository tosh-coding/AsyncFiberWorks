using System;
using System.Collections.Generic;
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
        private List<Func<TMessage, Task<TAck>>> _copied = new List<Func<TMessage, Task<TAck>>>();
        private bool _publishing;

        /// <summary>
        /// Add a message handler.
        /// </summary>
        /// <param name="action">A message handler.</param>
        /// <returns>Function for removing the handler.</returns>
        public IDisposable AddHandler(Func<TMessage, Task<TAck>> action)
        {
            lock (_lock)
            {
                _handlers.AddLast(action);
            }

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
            lock (_lock)
            {
                if (_publishing)
                {
                    throw new InvalidOperationException("Cannot be executed in parallel.");
                }
                _publishing = true;
                _copied.Clear();
                _copied.AddRange(_handlers);
            }
            try
            {
                await control.OnPublish(msg, _copied).ConfigureAwait(false);
            }
            finally
            {
                lock (_lock)
                {
                    _publishing = false;
                }
            }
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
