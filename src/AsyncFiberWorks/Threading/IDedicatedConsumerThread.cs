namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Provide "Add Action" and "Stop Thread" operations for a worker thread.
    /// </summary>
    public interface IDedicatedConsumerThread : IThreadWork, IConsumerThread
    {
    }
}
