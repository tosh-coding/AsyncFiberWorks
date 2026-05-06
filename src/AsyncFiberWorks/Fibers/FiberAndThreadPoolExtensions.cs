using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Linkage of Fiber and ThreadPool.
    /// </summary>
    public static class FiberAndThreadPoolExtensions
    {
        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified exception handler.
        /// </summary>
        /// <param name="threadPool">Thread pool used to execute queued actions.</param>
        /// <param name="exceptionHandler">Optional handler invoked when an action throws an exception.</param>
        /// <returns>Created fiber.</returns>
        public static PoolFiber CreateFiber(this IThreadPool threadPool, IActionExceptionHandler exceptionHandler = null)
        {
            return new PoolFiber(threadPool, exceptionHandler);
        }
    }
}
