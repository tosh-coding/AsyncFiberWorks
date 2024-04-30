using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver invoking interface.
    /// </summary>
    public interface IAsyncActionInvoker
    {
        /// <summary>
        /// Invoke all actions.
        /// </summary>
        Task Invoke();
    }
}
