namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Context switcher for IThreadPool.
    /// </summary>
    public static class ThreadPoolSwitcher
    {
        /// <summary>
        /// Switch the current context to the specified one.
        /// </summary>
        /// <param name="threadPool"></param>
        /// <returns></returns>
        public static ThreadPoolNotifyCompletion SwitchTo(this IThreadPool threadPool)
        {
            return new ThreadPoolNotifyCompletion(threadPool);
        }
    }
}
