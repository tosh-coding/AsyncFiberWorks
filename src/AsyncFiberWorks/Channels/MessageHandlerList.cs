using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// List of message handlers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class MessageHandlerList<T>
    {
        private event Action<T> _handlers;

        /// <summary>
        /// Add a message handler.
        /// </summary>
        /// <param name="action">A message handler.</param>
        /// <returns>Function for removing the handler.</returns>
        public Unsubscriber AddHandler(Action<T> action)
        {
            _handlers += action;

            var unsubscriber = new Unsubscriber(() => {
                this._handlers -= action;
            });

            return unsubscriber;
        }

        /// <summary>
        /// Call all message handlers.
        /// </summary>
        /// <param name="msg">A message.</param>
        /// <returns>True if one or more handlers have been called.</returns>
        public bool Publish(T msg)
        {
            var evnt = _handlers; // copy reference for thread safety
            if (evnt != null)
            {
                evnt(msg);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Number of handlers.
        /// </summary>
        public int Count
        {
            get
            {
                var evnt = _handlers; // copy reference for thread safety
                return evnt == null ? 0 : evnt.GetInvocationList().Length;
            }
        }
    }
}
