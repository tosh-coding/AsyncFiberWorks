namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Work to be performed by a thread.
    /// </summary>
    public interface IDedicatedConsumerThreadWork : IThreadWork, IConsumerThread
    {
    }
}
