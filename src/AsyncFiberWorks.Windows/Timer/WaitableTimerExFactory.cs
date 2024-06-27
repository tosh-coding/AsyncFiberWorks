using AsyncFiberWorks.Core;
using System;
using System.Threading;

namespace AsyncFiberWorks.Windows.Timer
{
    /// <summary>
    /// Timer using WaitableTimerEx in Windows.
    /// </summary>
    public class WaitableTimerExFactory : ITimerFactory
    {
        /// <summary>
        /// Start a timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstIntervalMs">Timer wait time.</param>
        /// <returns>A handle to cancel the timer.</returns>
        public IDisposable Schedule(Action action, long firstIntervalMs)
        {
            var disposer = new Unsubscriber();
            var waitableTimer = new WaitableTimerEx();
            waitableTimer.Set(firstIntervalMs * -10000L);
            var tmpHandle = ThreadPool.RegisterWaitForSingleObject(waitableTimer, (state, timeout) =>
            {
                disposer.Dispose();
                action();
            }, null, millisecondsTimeOutInterval: Timeout.Infinite, executeOnlyOnce: true);
            disposer.AppendDisposable(new OneTimeDisposer(() => tmpHandle.Unregister(null)));
            disposer.AppendDisposable(waitableTimer);
            return disposer;
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

            var disposer = new Unsubscriber();
            var waitableTimer = new WaitableTimerEx(manualReset: false);
            waitableTimer.Set(firstIntervalMs * -10000L);
            var tmpHandle = ThreadPool.RegisterWaitForSingleObject(waitableTimer, (state, timeout) =>
            {
                action();
                waitableTimer.Set(intervalMs * -10000L);
            }, null, millisecondsTimeOutInterval: Timeout.Infinite, executeOnlyOnce: false);
            disposer.AppendDisposable(new OneTimeDisposer(() => tmpHandle.Unregister(null)));
            disposer.AppendDisposable(waitableTimer);
            return disposer;
        }
    }
}
