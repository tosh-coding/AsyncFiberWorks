using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Methods for scheduling actions that will be executed in the future.
    /// </summary>
    public static class TimerExtensions
    {
        /// <summary>
        /// Schedules an action to be executed once.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="timerFactory"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable Schedule(this IExecutionContext fiber, Action action, long firstInMs, IOneshotTimerFactory timerFactory = null)
        {
            if (timerFactory == null)
            {
                timerFactory = new ThreadingTimerFactory();
            }
            return timerFactory.Schedule(() => fiber.Enqueue(action), firstInMs);
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <param name="timerFactory"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IExecutionContext fiber, Action action, long firstInMs, long regularInMs, IIntervalTimerFactory timerFactory = null)
        {
            if (regularInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularInMs));
            }

            if (timerFactory == null)
            {
                timerFactory = new ThreadingTimerFactory();
            }
            return timerFactory.ScheduleOnInterval(() => fiber.Enqueue(action), firstInMs, regularInMs);
        }

        /// <summary>
        /// Schedules a task to be executed once.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs"></param>
        /// <param name="timerFactory"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable Schedule(this IAsyncExecutionContext fiber, Func<Task> func, long firstInMs, IOneshotTimerFactory timerFactory = null)
        {
            if (timerFactory == null)
            {
                timerFactory = new ThreadingTimerFactory();
            }
            return timerFactory.Schedule(() => fiber.EnqueueTask(func), firstInMs);
        }

        /// <summary>
        /// Schedule a task to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <param name="timerFactory"></param>
        /// <returns>A handle to cancel the timer.</returns>
        public static IDisposable ScheduleOnInterval(this IAsyncExecutionContext fiber, Func<Task> func, long firstInMs, long regularInMs, IIntervalTimerFactory timerFactory = null)
        {
            if (regularInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularInMs));
            }

            if (timerFactory == null)
            {
                timerFactory = new ThreadingTimerFactory();
            }
            return timerFactory.ScheduleOnInterval(() => fiber.EnqueueTask(func), firstInMs, regularInMs);
        }
    }
}
