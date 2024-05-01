using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver invoking interface.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    /// <typeparam name="TRet">Type of return value.</typeparam>
    public interface IAsyncActionInvoker<TArg, TRet>
    {
        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <returns>Tasks waiting for call completion.</returns>
        Task Invoke(TArg arg);
    }
}
