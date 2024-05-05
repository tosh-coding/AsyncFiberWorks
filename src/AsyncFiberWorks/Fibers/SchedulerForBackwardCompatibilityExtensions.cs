using AsyncFiberWorks.Core;
using AsyncFiberWorks.Procedures;
using System;
using System.Threading.Tasks;

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

        /// <summary>
        /// Schedules a task to be executed once.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs"></param>
        /// <param name="executor"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable Schedule(this IAsyncFiber fiber, Func<Task> func, long firstInMs, IAsyncExecutorSingle executor = null)
        {
            if (executor == null)
            {
                executor = AsyncSimpleExecutorSingle.Instance;
            }
            return OneshotTimerAction.StartNew(() => fiber.Enqueue(func), firstInMs);
        }

        /// <summary>
        /// Schedule a task to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <param name="executor">If null, a non-reentrant executor is used.</param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IAsyncFiber fiber, Func<Task> func, long firstInMs, long regularInMs, IAsyncExecutorSingle executor = null)
        {
            if (executor == null)
            {
                executor = new NonReentrantAsyncExecutorSingle();
            }

            if (regularInMs <= 0)
            {
                return Schedule(fiber, func, firstInMs);
            }
            else
            {
                return IntervalTimerAction.StartNew(() => fiber.Enqueue(async () =>
                {
                    await executor.Execute(func).ConfigureAwait(false);
                }), firstInMs, regularInMs);
            }
        }
    }
}
