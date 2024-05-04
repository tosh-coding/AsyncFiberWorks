using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Consumer context. Tasks are waited for completion one at a time.
    /// </summary>
    public interface IAsyncFiber
    {
        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        void Enqueue(Func<Task> func);
    }
}
