using AsyncFiberWorks.Core;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Task invoke interface.
    /// </summary>
    public interface ISequentialTaskInvoker
    {
        /// <summary>
        /// Invoke all tasks sequentially.
        /// </summary>
        /// <returns>A task that waits for tasks to be performed.</returns>
        Task InvokeSequentialAsync();
    }
}
