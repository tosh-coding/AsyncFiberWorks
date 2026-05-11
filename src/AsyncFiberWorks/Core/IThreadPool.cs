using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// A thread pool for executing asynchronous actions.
    /// </summary>
    public interface IThreadPool
    {
        /// <summary>
        /// Enqueue the action to a thread pool.
        /// </summary>
        /// <param name="callback">The callback method to be executed by a thread pool thread.</param>
        /// <param name="state">An object containing information to be used by the callback method. </param>
        void Queue(WaitCallback callback, object state = null);
    }
}
