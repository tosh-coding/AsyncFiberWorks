using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Work to be performed by a thread.
    /// </summary>
    public interface IDedicatedConsumerThreadWork : IExecutionContext
    {
        /// <summary>
        /// Perform pending actions.
        /// </summary>
        /// <returns>Still in operation. False if already stopped.</returns>
        bool ExecuteNextBatch();

        /// <summary>
        /// Stop working.
        /// Once stopped, it cannot be restarted.
        /// </summary>
        void Stop();
    }
}
