using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public class PoolFiber : FiberWithDisposableList
    {
        private readonly PoolFiberSlim _poolFiberSlim;

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        public PoolFiber(IThreadPool pool, IExecutor executor)
            : this(new PoolFiberSlim(pool, executor))
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        public PoolFiber(IExecutor executor) 
            : this(new PoolFiberSlim(executor))
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor.
        /// </summary>
        public PoolFiber() 
            : this(new PoolFiberSlim())
        {
        }

        /// <summary>
        /// Create a pool fiber.
        /// </summary>
        /// <param name="poolFiberSlim"></param>
        private PoolFiber(PoolFiberSlim poolFiberSlim)
            : base(poolFiberSlim)
        {
            _poolFiberSlim = poolFiberSlim;
        }

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor,
        /// and call the Start method.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static PoolFiber StartNew(IThreadPool pool, IExecutor executor)
        {
            var fiber = new PoolFiber(pool, executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool, and call the Start method.
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static PoolFiber StartNew(IExecutor executor)
        {
            var fiber = new PoolFiber(executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor,
        /// and call the Start method.
        /// </summary>
        /// <returns></returns>
        public static PoolFiber StartNew()
        {
            var fiber = new PoolFiber();
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Start consuming actions.
        /// </summary>
        public override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Stop consuming actions.
        /// </summary>
        public void Stop()
        {
            _poolFiberSlim.Stop();
        }

        /// <summary>
        /// Stops the fiber.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}