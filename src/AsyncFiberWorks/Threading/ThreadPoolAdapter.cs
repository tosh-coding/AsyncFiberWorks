using AsyncFiberWorks.Core;
using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Wrapper that shows IDedicatedConsumerThreadWork as an IThreadPool.
    /// </summary>
    public class ThreadPoolAdapter : IThreadPool
    {
        private readonly IDedicatedConsumerThreadWorkQueue _queue;

        /// <summary>
        /// Create an IThreadPool wrapper by the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadPoolAdapter(IDedicatedConsumerThreadWorkQueue queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback">The callback method to be executed by a thread pool thread.</param>
        /// <param name="state">An object containing information to be used by the callback method.</param>
        public void Queue(WaitCallback callback, object state)
        {
            _queue.Enqueue(callback, state);
        }
    }
}
