using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Sequential executor of actions.
    /// </summary>
    public interface IAsyncExecutionContext
    {
        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action.</param>
        void Enqueue(Action<IFiberExecutionEventArgs, object> action, object state);
    }
}