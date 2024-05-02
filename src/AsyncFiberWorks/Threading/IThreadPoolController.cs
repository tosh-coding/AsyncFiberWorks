namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Start and stop threads.
    /// </summary>
    public interface IThreadPoolController
    {
        /// <summary>
        /// Start the threads.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the threads.
        /// Once stopped, it cannot be restarted.
        /// </summary>
        void Stop();
    }
}
