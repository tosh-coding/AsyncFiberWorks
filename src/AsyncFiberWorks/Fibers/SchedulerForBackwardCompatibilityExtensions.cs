using AsyncFiberWorks.Channels;
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
        public static IDisposableSubscriptionRegistry Schedule(this IExecutionContext fiber, Action action, long firstInMs)
        {
            var unsubscriberList = new Unsubscriber();
            var timer = TimerAction.StartNew(() => fiber.Enqueue(action), () => unsubscriberList.Dispose(), firstInMs, Timeout.Infinite);
            unsubscriberList.AddDisposable(timer);
            return unsubscriberList;
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>a handle to cancel the timer.</returns>
        public static IDisposableSubscriptionRegistry ScheduleOnInterval(this IExecutionContext fiber, Action action, long firstInMs, long regularInMs)
        {
            var unsubscriberList = new Unsubscriber();
            var timer = TimerAction.StartNew(() => fiber.Enqueue(action), () => unsubscriberList.Dispose(), firstInMs, regularInMs);
            unsubscriberList.AddDisposable(timer);
            return unsubscriberList;
        }
    }
}
