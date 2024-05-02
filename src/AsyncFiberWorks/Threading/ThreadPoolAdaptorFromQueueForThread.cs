using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Adapter from IQueueForThread to IThreadPool.
    /// </summary>
    public class ThreadPoolAdaptorFromQueueForThread : IThreadPool, IConsumerQueueForThread
    {
        private readonly IQueueForThread _queue;

        /// <summary>
        /// Create a pseudo-thread pool with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadPoolAdaptorFromQueueForThread(IQueueForThread queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Create a pseudo-thread pool with BlockingCollectionQueue.
        /// </summary>
        public ThreadPoolAdaptorFromQueueForThread()
            : this(new BlockingCollectionQueue())
        {
        }

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback"></param>
        public void Queue(WaitCallback callback)
        {
            _queue.Enqueue(() => callback(null));
        }

        /// <summary>
        /// Execute actions until stopped.
        /// </summary>
        public void Run()
        {
            _queue.Run();
        }

        /// <summary>
        /// Stop consuming the actions.
        /// </summary>
        public void Stop()
        {
            _queue.Stop();
        }
    }
}
