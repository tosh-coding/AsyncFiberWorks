using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    public class AsyncActionDriver : IAsyncActionDriver
    {
        private readonly AsyncActionList _actions = new AsyncActionList();
        private readonly IAsyncExecutorBatch _executorBatch;
        private readonly IAsyncExecutor _executorSingle;

        /// <summary>
        /// Create a driver with custom executors.
        /// </summary>
        /// <param name="executorBatch"></param>
        /// <param name="executorSingle"></param>
        public AsyncActionDriver(IAsyncExecutorBatch executorBatch, IAsyncExecutor executorSingle = null)
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
            : this(AsyncSimpleExecutorBatch.Instance, null)
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
                var maskableFilter = new AsyncMaskableExecutor();
                var disposable = _actions.AddHandler(() => maskableFilter.Execute(() => _executorSingle.Execute(action)));
                return new Unsubscriber(() =>
                {
                    maskableFilter.IsEnabled = false;
                    disposable.Dispose();
                });
            }
            else
            {
                var maskableFilter = new AsyncMaskableExecutor();
                var disposable = _actions.AddHandler(() => maskableFilter.Execute(action));
                return new Unsubscriber(() =>
                {
                    maskableFilter.IsEnabled = false;
                    disposable.Dispose();
                });
            }
        }

        /// <summary>
        /// <see cref="IAsyncActionInvoker.Invoke"/>
        /// </summary>
        public async Task Invoke()
        {
            await _actions.Invoke(_executorBatch).ConfigureAwait(false);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
