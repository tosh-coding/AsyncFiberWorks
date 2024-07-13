using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// One-shot timer.
    /// </summary>
    public interface IOneshotTimer : IDisposable
    {
        /// <summary>
        /// Start a timer.
        /// For TimerExtensions implementation.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="state">Arguments passed when that callback is invoked.</param>
        /// <param name="intervalMs">Timer wait time. Must be greater than or equal to 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        void InternalSchedule(Action<object> action, object state, int intervalMs, CancellationToken token = default);
    }
}
