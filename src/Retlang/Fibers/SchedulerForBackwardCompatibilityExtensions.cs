using Retlang.Core;
using System;
using System.Threading;

namespace Retlang.Fibers
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
        public static IDisposable Schedule(this IFiberWithFallbackRegistry fiber, Action action, long firstInMs)
        {
            return TimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, Timeout.Infinite, fiber.FallbackDisposer);
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>a handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IFiberWithFallbackRegistry fiber, Action action, long firstInMs, long regularInMs)
        {
            return TimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, regularInMs, fiber.FallbackDisposer);
        }
    }
}
