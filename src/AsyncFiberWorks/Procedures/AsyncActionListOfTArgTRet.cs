using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Channels;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of actions.
    /// </summary>
    /// <typeparam name="TArg">Type of argument.</typeparam>
    /// <typeparam name="TRet">Type of return value.</typeparam>
    internal sealed class AsyncActionList<TArg, TRet>
    {
        private object _lock = new object();
        private LinkedList<Func<TArg, Task<TRet>>> _handlers = new LinkedList<Func<TArg, Task<TRet>>>();
        private List<Func<TArg, Task<TRet>>> _copied = new List<Func<TArg, Task<TRet>>>();
        private bool _publishing;

        /// <summary>
        /// Add an action.
        /// </summary>
        /// <param name="action">An action.</param>
        /// <returns>Function for removing the action.</returns>
        public IDisposable AddHandler(Func<TArg, Task<TRet>> action)
        {
            lock (_lock)
            {
                _handlers.AddLast(action);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                lock (_lock)
                {
                    _handlers.Remove(action);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="executor"></param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task Invoke(TArg arg, IAsyncExecutor<TArg, TRet> executor)
        {
            lock (_lock)
            {
                if (_publishing)
                {
                    throw new InvalidOperationException("Cannot be executed in parallel.");
                }
                _publishing = true;
                _copied.Clear();
                _copied.AddRange(_handlers);
            }
            try
            {
                await executor.Execute(arg, _copied).ConfigureAwait(false);
            }
            finally
            {
                lock (_lock)
                {
                    _publishing = false;
                }
            }
        }

        /// <summary>
        /// Number of handlers.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _handlers.Count;
                }
            }
        }
    }
}
