﻿using AsyncFiberWorks.Core;
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
        public static IDisposable Schedule(this ISubscribableFiber fiber, Action action, long firstInMs)
        {
            var timerAction = TimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, Timeout.Infinite);
            fiber.BeginSubscriptionAndSetUnsubscriber(timerAction);
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
        public static IDisposable ScheduleOnInterval(this ISubscribableFiber fiber, Action action, long firstInMs, long regularInMs)
        {
            var timerAction = TimerAction.StartNew(() => fiber.Enqueue(action), firstInMs, regularInMs);
            fiber.BeginSubscriptionAndSetUnsubscriber(timerAction);
            return timerAction;
        }
    }
}
