using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.FiberSchedulers
{
    /// <summary>
    /// Methods for scheduling actions that will be executed in the future.
    /// </summary>
    public static class TimerExtensions
    {
        /// <summary>
        /// Schedules an action to be executed once.
        /// </summary>
        /// <param name="timerFactory"></param>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable Schedule(this ITimerFactory timerFactory, IExecutionContext fiber, Action action, long firstInMs)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            return timerFactory.Schedule(() => fiber.Enqueue(action), firstInMs);
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="timerFactory"></param>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this ITimerFactory timerFactory, IExecutionContext fiber, Action action, long firstInMs, long regularInMs)
        {
            if (regularInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularInMs));
            }

            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            return timerFactory.ScheduleOnInterval(() => fiber.Enqueue(action), firstInMs, regularInMs);
        }

        /// <summary>
        /// Schedules a task to be executed once.
        /// </summary>
        /// <param name="timerFactory"></param>
        /// <param name="fiber"></param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable Schedule(this ITimerFactory timerFactory, IAsyncExecutionContext fiber, Func<Task> func, long firstInMs)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            return timerFactory.Schedule(() => fiber.EnqueueTask(func), firstInMs);
        }

        /// <summary>
        /// Schedule a task to be executed on a recurring interval.
        /// </summary>
        /// <param name="timerFactory"></param>
        /// <param name="fiber"></param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this ITimerFactory timerFactory, IAsyncExecutionContext fiber, Func<Task> func, long firstInMs, long regularInMs)
        {
            if (regularInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularInMs));
            }

            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            return timerFactory.ScheduleOnInterval(() => fiber.EnqueueTask(func), firstInMs, regularInMs);
        }
    }
}
