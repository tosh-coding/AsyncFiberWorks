using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Work to be performed by a thread.
    /// </summary>
    public interface IDedicatedConsumerThreadWork : IExecutionContext
    {
        /// <summary>
        /// Start working.
        /// Does not return from the call until it stops.
        /// </summary>
        void Run();

        /// <summary>
        /// Stop working.
        /// Once stopped, it cannot be restarted.
        /// </summary>
        void Stop();
    }
}
