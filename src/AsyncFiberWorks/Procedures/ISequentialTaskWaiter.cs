using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Use "await" to simply wait for the start of a task on a sequential task list.
    /// </summary>
    public interface ISequentialTaskWaiter
    {
        /// <summary>
        /// Wait for task execution.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        Task ExecutionStarted();
    }
}
