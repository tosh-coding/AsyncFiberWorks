using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncActionDriver<T> : IAsyncActionDriver<T>
    {
        private readonly AsyncActionList<T> _actions = new AsyncActionList<T>();
        private readonly IAsyncExecutor<T> _executor;

        /// <summary>
        /// Create a driver.
        /// </summary>
        /// <param name="executor"></param>
        public AsyncActionDriver(IAsyncExecutor<T> executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncActionDriver()
            : this(new DefaultAsyncExecutor<T>())
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
            await _actions.Invoke(arg, _executor);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
