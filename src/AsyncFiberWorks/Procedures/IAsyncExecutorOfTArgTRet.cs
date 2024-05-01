using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Executes actions.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    /// <typeparam name="TRet">Type of return value.</typeparam>
    public interface IAsyncExecutor<TArg, TRet>
    {
        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="actions">A list of actions.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        Task Execute(TArg arg, IReadOnlyList<Func<TArg, Task<TRet>>> actions);
    }
}
