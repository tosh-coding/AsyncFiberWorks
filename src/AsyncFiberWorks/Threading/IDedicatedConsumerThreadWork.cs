using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Work to be performed by a thread.
    /// </summary>
    public interface IDedicatedConsumerThreadWork : IDedicatedConsumerThreadWorkQueue, IDedicatedConsumerThreadWorkExecution
    {
    }

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

    /// <summary>
    /// Execution of tasks on a dedicated consumer thread.
    /// </summary>
    public interface IDedicatedConsumerThreadWorkExecution
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
