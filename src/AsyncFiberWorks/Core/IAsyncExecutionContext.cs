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
        /// <param name="func">Task generator. This is done after a pause in the fiber. The generated task is monitored and takes action to resume after completion.</param>
        void Enqueue(Func<Task<Action>> func);
    }
}