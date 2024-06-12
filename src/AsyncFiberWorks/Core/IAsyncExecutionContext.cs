using System;
using System.Threading.Tasks;

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
        void Enqueue(Action<FiberExecutionEventArgs> action);
    }
}