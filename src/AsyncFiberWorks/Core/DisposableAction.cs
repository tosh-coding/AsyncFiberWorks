using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Wrap the discard process as an IDisposable implementation class.
    /// </summary>
    public class DisposableAction: IDisposable
    {
        private Action _action;

        /// <summary>
        /// Set an action.
        /// </summary>
        /// <param name="action">Discarding action.</param>
        public DisposableAction(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Invoke that Action.
        /// </summary>
        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}
