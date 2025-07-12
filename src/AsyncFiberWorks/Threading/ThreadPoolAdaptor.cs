using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Wrapper that shows IDedicatedConsumerThread as an IThreadPool.
    /// </summary>
    public class ThreadPoolAdaptor : IThreadPool
    {
        private readonly IDedicatedConsumerThreadWork _queue;

        /// <summary>
        /// Create an IThreadPool wrapper by the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadPoolAdaptor(IDedicatedConsumerThreadWork queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Create an IThreadPool wrapper using some IDedicatedConsumerThread.
        /// </summary>
        public ThreadPoolAdaptor()
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
        /// Start consumption. Continue until stopped.
        /// Make the current thread available as an IThreadPool.
        /// </summary>
        public void Run()
        {
            while (_queue.ExecuteNextBatch()) { }
        }

        /// <summary>
        /// Stop consumption.
        /// </summary>
        public void Stop()
        {
            _queue.Stop();
        }
    }
}
