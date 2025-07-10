using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// The lifespan of the thread matches that of the fiber.
    /// </summary>
    public sealed class AnotherFiberDisposable : IFiber, IDisposable
    {
        private readonly UserThreadPool _threadPool;
        private readonly PoolFiber _fiber;

        /// <summary>
        /// Create a pool fiber with a new UserThreadPool.
        /// </summary>
        /// <param name="threadName"></param>
        public AnotherFiberDisposable(string threadName = null)
        {
            _threadPool = new UserThreadPool(numberOfThread: 1, threadName);
            _threadPool.Start();
            _fiber = new PoolFiber(_threadPool);
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Stop()
        {
            _threadPool.Stop();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Dispose()
        {
            _threadPool.Dispose();
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _fiber.Enqueue(action);
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action<FiberExecutionEventArgs> action)
        {
            _fiber.Enqueue(action);
        }
    }
}
