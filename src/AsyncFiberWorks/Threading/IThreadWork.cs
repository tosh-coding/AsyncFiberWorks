namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Start and stop thread work.
    /// </summary>
    public interface IThreadWork
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
