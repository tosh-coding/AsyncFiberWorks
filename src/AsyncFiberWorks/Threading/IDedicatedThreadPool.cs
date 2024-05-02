namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Provide "Add Action" and "Stop Threads" operations for a thread pool.
    /// </summary>
    public interface IDedicatedThreadPool : IThreadWork, IThreadPool
    {
    }
}
