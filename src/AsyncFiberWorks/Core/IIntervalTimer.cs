using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Repeating timer.
    /// </summary>
    public interface IIntervalTimer : IDisposable
    {
        /// <summary>
        /// Start a repeating timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstIntervalMs">Initial wait time. Must be greater than or equal to 0.</param>
        /// <param name="intervalMs">The waiting interval time after the second time. Must be greater than 0.</param>
        /// <param name="cancellation">A handle to cancel the timer.</param>
        void ScheduleOnInterval(Action action, int firstIntervalMs, int intervalMs, CancellationToken cancellation = default);
    }
}
