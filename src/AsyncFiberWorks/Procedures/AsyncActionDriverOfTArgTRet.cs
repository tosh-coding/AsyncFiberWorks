using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    /// <typeparam name="TRet">Type of return value.</typeparam>
    public class AsyncActionDriver<TArg, TRet> : IAsyncActionDriver<TArg, TRet>
    {
        private readonly AsyncActionList<TArg, TRet> _channel = new AsyncActionList<TArg, TRet>();
        private readonly IAsyncExecutor<TArg, TRet> _executor;

        /// <summary>
        /// Create a driver.
        /// </summary>
        /// <param name="executor"></param>
        public AsyncActionDriver(IAsyncExecutor<TArg, TRet> executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<TArg, Task<TRet>> action)
        {
            return _channel.AddHandler(action);
        }

        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <returns>Tasks waiting for call completion.</returns>
        public async Task Invoke(TArg arg)
        {
            await _channel.Invoke(arg, _executor);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _channel.Count; } }
    }
}
