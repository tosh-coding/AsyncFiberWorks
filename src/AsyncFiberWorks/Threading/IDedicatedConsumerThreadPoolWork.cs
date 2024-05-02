namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Work to be performed by threads.
    /// </summary>
    public interface IDedicatedConsumerThreadPoolWork : IThreadPool
    {
        /// <summary>
        /// Work list. The same number of worker threads are required.
        /// </summary>
        IThreadWork[] Works { get; }
    }
}
