using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// A thread pool for executing asynchronous actions.
    /// </summary>
    public interface IThreadPool
    {
        /// <summary>
        /// Enqueue action for execution.
        /// The action is stored in an internal queue and returns immediately from the call.
        /// Queued actions are processed in parallel.
        /// </summary>
        /// <param name="callback"></param>
        void Queue(WaitCallback callback);
    }
}