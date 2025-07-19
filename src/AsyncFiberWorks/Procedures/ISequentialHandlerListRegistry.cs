using AsyncFiberWorks.Fibers;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Message handler list registration interface.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public interface ISequentialHandlerListRegistry<TMessage>
    {
        /// <summary>
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler. The return value is the processed flag. If this flag is true, subsequent handlers are not called.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        IDisposable Add(Func<TMessage, bool> handler, IFiber context = null);

        /// <summary>
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler. The return value is the processed flag. If this flag is true, subsequent handlers are not called.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        IDisposable Add(Func<TMessage, Task<bool>> handler, IFiber context = null);
    }
}
