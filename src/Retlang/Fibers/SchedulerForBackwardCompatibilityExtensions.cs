﻿using Retlang.Core;
using System;

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
        public static IDisposable Schedule(this IFiber fiber, Action action, long firstInMs)
        {
            return TimerAction.StartNew(fiber, action, firstInMs);
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>a handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IFiber fiber, Action action, long firstInMs, long regularInMs)
        {
            return TimerAction.StartNew(fiber, action, firstInMs, regularInMs);
        }
    }
}