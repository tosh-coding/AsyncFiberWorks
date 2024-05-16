using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Generator of One-time timers.
    /// </summary>
    public interface IOneshotTimerFactory
    {
        /// <summary>
        /// Start a timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="intervalMs">Timer wait time.</param>
        /// <returns>A handle to cancel the timer.</returns>
        IDisposable Schedule(Action action, long intervalMs);
    }
}
