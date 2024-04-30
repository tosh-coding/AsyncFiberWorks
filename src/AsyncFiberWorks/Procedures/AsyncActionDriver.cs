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
        private readonly IAsyncExecutor _executor;

        /// <summary>
        /// Create a driver.
        /// </summary>
        /// <param name="executor"></param>
        public AsyncActionDriver(IAsyncExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncActionDriver()
            : this(new DefaultAsyncExecutor())
        {
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<Task> action)
        {
            return _actions.AddHandler(action);
        }

        /// <summary>
        /// <see cref="IAsyncActionInvoker.Invoke"/>
        /// </summary>
        public async Task Invoke()
        {
            await _actions.Invoke(_executor);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
