using AsyncFiberWorks.Core;
using System;
using System.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Methods for scheduling actions that will be executed in the future.
    /// </summary>
    public static class SchedulerForBackwardCompatibilityExtensions
    {
        /// <summary>
        /// Schedules an action to be executed once.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <returns>a handle to cancel the timer.</returns>
        public static IDisposable Schedule(this IExecutionContextWithPossibleStoppage fiber, Action action, long firstInMs)
        {
            var unsubscriber = fiber.CreateSubscription();
            Action cbOnTimerDisposing = () => { unsubscriber.Dispose(); };
            var timerAction = TimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, Timeout.Infinite, cbOnTimerDisposing);
            unsubscriber.Add(() => timerAction.Dispose());
            return timerAction;
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>a handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IExecutionContextWithPossibleStoppage fiber, Action action, long firstInMs, long regularInMs)
        {
            var unsubscriber = fiber.CreateSubscription();
            Action cbOnTimerDisposing = () => { unsubscriber.Dispose(); };
            var timerAction = TimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, regularInMs, cbOnTimerDisposing);
            unsubscriber.Add(() => timerAction.Dispose());
            return timerAction;
        }
    }
}
