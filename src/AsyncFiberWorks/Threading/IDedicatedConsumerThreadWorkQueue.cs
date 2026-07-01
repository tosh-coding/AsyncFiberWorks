using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Enqueue for dedicated consumer thread work.
    /// </summary>
    public interface IDedicatedConsumerThreadWorkQueue
    {
        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        void Enqueue(WaitCallback action, object state);
    }
}
