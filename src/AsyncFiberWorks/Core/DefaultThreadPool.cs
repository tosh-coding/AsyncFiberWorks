using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Default implementation that uses the .NET thread pool.
    /// </summary>
    public class DefaultThreadPool : IThreadPool
    {
        /// <summary>
        /// The singleton instance of DefaultThreadPool.
        /// </summary>
        public static readonly DefaultThreadPool Instance = new DefaultThreadPool();

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback"></param>
        public void Queue(WaitCallback callback)
        {
            if (!ThreadPool.QueueUserWorkItem(callback))
            {
                throw new QueueFullException("Unable to add item to pool: " + callback.Target);
            }
        }
    }
}