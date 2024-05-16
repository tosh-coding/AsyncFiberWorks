using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Wrapper class for System.Threading.Timer.
    /// </summary>
    public class ThreadingTimerFactory : IOneshotTimerFactory, IIntervalTimerFactory
    {
        readonly IExecutor _executor;

        /// <summary>
        /// Specify the executor at the time of timer expiration.
        /// </summary>
        /// <param name="executor"></param>
        public ThreadingTimerFactory(IExecutor executor = null)
        {
            _executor = executor;
        }

        /// <summary>
        /// Start a timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="intervalMs">Timer wait time.</param>
        /// <returns>A handle to cancel the timer.</returns>
        public IDisposable Schedule(Action action, long intervalMs)
        {
            if (_executor != null)
            {
                return OneshotTimerAction.StartNew(() => _executor.Execute(action), intervalMs);
            }
            else
            {
                return OneshotTimerAction.StartNew(action, intervalMs);
            }
        }

        /// <summary>
        /// Start a repeating timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstIntervalMs">Initial wait time.</param>
        /// <param name="intervalMs">The waiting interval time after the second time.</param>
        /// <returns>A handle to cancel the timer.</returns>
        public IDisposable ScheduleOnInterval(Action action, long firstIntervalMs, long intervalMs)
        {
            if (intervalMs <= 0)
            {
                return Schedule(action, firstIntervalMs);
            }
            else
            {
                return IntervalTimerAction.StartNew(action, firstIntervalMs, intervalMs);
            }
        }
    }
}
