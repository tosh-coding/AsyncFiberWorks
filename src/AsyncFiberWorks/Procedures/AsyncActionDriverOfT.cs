using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.MessageFilters;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncActionDriver<T> : IAsyncActionDriver<T>
    {
        private readonly AsyncActionList<T> _actions = new AsyncActionList<T>();
        private readonly IAsyncExecutorBatch<T> _executorBatch;
        private readonly IAsyncExecutor<T> _executorSingle;

        /// <summary>
        /// Create a driver with custom executors.
        /// </summary>
        /// <param name="executorBatch"></param>
        /// <param name="executorSingle"></param>
        public AsyncActionDriver(IAsyncExecutorBatch<T> executorBatch, IAsyncExecutor<T> executorSingle = null)
        {
            if (executorBatch == null)
            {
                throw new ArgumentNullException(nameof(executorBatch));
            }
            _executorBatch = executorBatch;
            _executorSingle = executorSingle;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncActionDriver()
            : this(AsyncSimpleExecutorBatch<T>.Instance, null)
        {
        }

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<T, Task> action)
        {
            return _actions.AddHandler(action);
        }

        /// <summary>
        /// Invoke all actions.
        /// </summary>
        /// <param name="arg">An argument.</param>
        public async Task Invoke(T arg)
        {
            await _actions.Invoke(arg, _executorBatch, _executorSingle).ConfigureAwait(false);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
