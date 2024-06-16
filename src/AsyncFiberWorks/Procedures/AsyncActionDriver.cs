using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    public class AsyncActionDriver : IAsyncActionDriver
    {
        private readonly AsyncActionList _actions = new AsyncActionList();
        private readonly IAsyncExecutor _executorSingle;

        /// <summary>
        /// Create a driver with custom executors.
        /// </summary>
        /// <param name="executorSingle"></param>
        public AsyncActionDriver(IAsyncExecutor executorSingle = null)
        {
            _executorSingle = executorSingle;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncActionDriver()
            : this(null)
        {
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<Task> action)
        {
            if (_executorSingle != null)
            {
                return _actions.AddHandler(() => _executorSingle.Execute(action));
            }
            else
            {
                return _actions.AddHandler(action);
            }
        }

        /// <summary>
        /// <see cref="IAsyncActionInvoker.Invoke"/>
        /// </summary>
        public async Task Invoke()
        {
            await _actions.Invoke().ConfigureAwait(false);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
