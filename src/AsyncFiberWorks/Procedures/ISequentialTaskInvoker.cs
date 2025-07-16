using AsyncFiberWorks.Fibers;
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
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        /// <returns>A task that waits for tasks to be performed.</returns>
        Task InvokeSequentialAsync(IFiber defaultContext);
    }
}
