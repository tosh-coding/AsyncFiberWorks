using AsyncFiberWorks.Fibers;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Action driver invoking interface.
    /// </summary>
    public interface IActionDriverInvoker
    {
        /// <summary>
        /// Invoke all subscribers.
        /// </summary>
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        Task InvokeAsync(IFiber defaultContext);
    }
}
