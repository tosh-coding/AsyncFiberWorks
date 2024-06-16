using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of actions.
    /// It is not thread-safe.
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
            var maskableFilter = new ToggleFilter();
            Action safeAction = () => maskableFilter.Execute(action);

            _actions += safeAction;

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                _actions -= safeAction;
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
