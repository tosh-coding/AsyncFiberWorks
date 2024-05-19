using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Generator of repeating timers.
    /// </summary>
    public interface IIntervalTimerFactory
    {
        /// <summary>
        /// Start a repeating timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstIntervalMs">Initial wait time.</param>
        /// <param name="intervalMs">The waiting interval time after the second time.</param>
        /// <returns>A handle to cancel the timer.</returns>
        IDisposable ScheduleOnInterval(Action action, long firstIntervalMs, long intervalMs);
    }
}
