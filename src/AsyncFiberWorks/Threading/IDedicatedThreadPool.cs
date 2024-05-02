namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Thread pool that can be started and stopped.
    /// </summary>
    public interface IDedicatedThreadPool : IThreadPoolController, IThreadPool
    {
    }
}
