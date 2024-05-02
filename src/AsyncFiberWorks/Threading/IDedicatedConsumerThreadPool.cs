namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Provide "Add Action" and "Stop All Threads" operations for a thread pool.
    /// </summary>
    public interface IDedicatedConsumerThreadPool : IThreadPool
    {
        /// <summary>
        /// Work list. The same number of worker threads are required.
        /// </summary>
        IThreadWork[] Works { get; }
    }
}
