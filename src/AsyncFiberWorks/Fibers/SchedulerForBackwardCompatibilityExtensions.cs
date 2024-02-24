using AsyncFiberWorks.Core;
using System;

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
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable Schedule(this IExecutionContext fiber, Action action, long firstInMs)
        {
            return OneshotTimerAction.StartNew(() => fiber.Enqueue(action), firstInMs);
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IExecutionContext fiber, Action action, long firstInMs, long regularInMs)
        {
            if (regularInMs <= 0)
            {
                return Schedule(fiber, action, firstInMs);
            }
            else
            {
                return IntervalTimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, regularInMs);
            }
        }
    }
}
