using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public class PoolFiber : PoolFiberSlim, IFiber
    {
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        public PoolFiber(IThreadPool pool, IExecutor executor)
            : base(pool, executor)
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        public PoolFiber(IExecutor executor) 
            : base(executor)
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor.
        /// </summary>
        public PoolFiber()
            : base()
        {
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistryGetter.FallbackDisposer"/>
        /// </summary>
        public ISubscriptionRegistry FallbackDisposer
        {
            get { return _subscriptions; }
        }
    }
}
