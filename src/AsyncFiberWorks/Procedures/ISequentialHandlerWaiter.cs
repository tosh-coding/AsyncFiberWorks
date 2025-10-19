using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Use "await" to simply wait for the start of a task on a sequential handler list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISequentialHandlerWaiter<T>
    {
        /// <summary>
        /// Wait for handler execution.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        Task<ProcessedFlagEventArgs<T>> ExecutionStarted();
    }
}
