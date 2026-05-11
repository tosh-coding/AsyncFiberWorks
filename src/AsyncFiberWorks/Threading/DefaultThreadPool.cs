using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
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
        /// <param name="callback">The callback method to be executed by a thread pool thread.</param>
        /// <param name="state">An object containing information to be used by the callback method.</param>
        public void Queue(WaitCallback callback, object state)
        {
            if (!ThreadPool.QueueUserWorkItem(callback, state))
            {
                throw new QueueFullException("Unable to add item to pool: " + callback.Target);
            }
        }
    }
}