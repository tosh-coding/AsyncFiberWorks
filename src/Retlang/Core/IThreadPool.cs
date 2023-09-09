using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// A thread pool for executing asynchronous actions.
    /// </summary>
    public interface IThreadPool
    {
        /// <summary>
        /// Enqueue action to the thread pool for execution.
        /// They are shared threads and should not be blocked.
        /// </summary>
        /// <param name="callback"></param>
        void Queue(WaitCallback callback);
    }
}