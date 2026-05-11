using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Extension of a thread pool.
    /// </summary>
    public static class IThreadPoolExtensions
    {
        /// <summary>
        /// Enqueue the action to a thread pool.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="action">The callback method to be executed by a thread pool thread.</param>
        public static void Queue(this IThreadPool me, Action action)
        {
            me.Queue((state) =>
            {
                ((Action)state)?.Invoke();
            }, action);
        }
    }
}
