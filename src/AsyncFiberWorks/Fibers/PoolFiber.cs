using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public class PoolFiber : IFiber, IAsyncExecutionContext, ISubscriptionRegistryViewing
    {
        private readonly PoolFiberSlim _poolFiberSlim;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        public PoolFiber(IThreadPool pool, IExecutor executor)
        {
            _poolFiberSlim = new PoolFiberSlim(pool, executor);
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        public PoolFiber(IExecutor executor)
        {
            _poolFiberSlim = new PoolFiberSlim(executor);
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor.
        /// </summary>
        public PoolFiber()
        {
            _poolFiberSlim = new PoolFiberSlim();
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistry.BeginSubscription"/>
        /// </summary>
        /// <returns></returns>
        public Unsubscriber BeginSubscription()
        {
            return _subscriptions.BeginSubscription();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistryViewing.NumSubscriptions"/>
        /// </summary>
        public int NumSubscriptions
        {
            get { return _subscriptions.NumSubscriptions; }
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _poolFiberSlim.Enqueue(action);
        }

        /// <summary>
        /// Pauses the consumption of the task queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Pause was called twice.</exception>
        public void Pause()
        {
            _poolFiberSlim.Pause();
        }

        /// <summary>
        /// Resumes consumption of a paused task queue.
        /// </summary>
        /// <param name="action">The action to be taken immediately after the resume.</param>
        /// <exception cref="InvalidOperationException">Resume was called in the unpaused state.</exception>
        public void Resume(Action action)
        {
            _poolFiberSlim.Resume(action);
        }

        /// <summary>
        /// Enqueue a single task.
        /// </summary>
        /// <param name="func">Task generator. This is done after a pause in the fiber. The generated task is monitored and takes action to resume after completion.</param>
        public void Enqueue(Func<Task<Action>> func)
        {
            _poolFiberSlim.Enqueue(func);
        }
    }
}
