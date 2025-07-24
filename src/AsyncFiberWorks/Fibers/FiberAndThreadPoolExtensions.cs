using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Linkage of Fiber and ThreadPool.
    /// </summary>
    public static class FiberAndThreadPoolExtensions
    {
        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="threadPool"></param>
        /// <param name="executor"></param>
        /// <returns>Created fiber.</returns>
        public static PoolFiber CreateFiber(this IThreadPool threadPool, IActionExecutor executor = null)
        {
            return new PoolFiber(threadPool, executor);
        }
    }
}
