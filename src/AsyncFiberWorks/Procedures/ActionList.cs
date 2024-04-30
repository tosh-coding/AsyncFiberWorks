using System;
using AsyncFiberWorks.Channels;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of actions.
    /// </summary>
    internal sealed class ActionList
    {
        private event Action _actions;

        /// <summary>
        /// Add an action.
        /// </summary>
        /// <param name="action">An action.</param>
        /// <returns>Function for removing the action.</returns>
        public IDisposable AddHandler(Action action)
        {
            _actions += action;

            var unsubscriber = new Unsubscriber(() =>
            {
                _actions -= action;
            });

            return unsubscriber;
        }

        /// <summary>
        /// Invoke all actions.
        /// </summary>
        public void Invoke()
        {
            var act = _actions; // copy reference for thread safety
            if (act != null)
            {
                act();
            }
        }

        /// <summary>
        /// Number of actions.
        /// </summary>
        public int Count
        {
            get
            {
                var evnt = _actions; // copy reference for thread safety
                return evnt == null ? 0 : evnt.GetInvocationList().Length;
            }
        }
    }
}
