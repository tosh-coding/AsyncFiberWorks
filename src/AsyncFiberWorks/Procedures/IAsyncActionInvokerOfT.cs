using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver invoking interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncActionInvoker<T>
    {
        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <returns>Tasks waiting for call completion.</returns>
        Task Invoke(T arg);
    }
}
