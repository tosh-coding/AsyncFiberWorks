using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Timers
{
    /// <summary>
    /// Methods for scheduling actions that will be executed in the future.
    /// </summary>
    public static class TimerExtensions
    {
        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="fiber">The context in which the action is executed.</param>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstInMs">Initial wait time. Must be greater than or equal to 0.</param>
        /// <param name="regularInMs">The waiting interval time after the second time. Must be greater than 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public static void ScheduleOnInterval(this IIntervalTimer timer, IExecutionContext fiber, Action action, int firstInMs, int regularInMs, CancellationToken token = default)
        {
            if (regularInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularInMs));
            }

            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            timer.ScheduleOnInterval(() => fiber.Enqueue(action), firstInMs, regularInMs, token);
        }

        /// <summary>
        /// Schedule a task to be executed on a recurring interval.
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="fiber">The context in which the action is executed.</param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs">Initial wait time. Must be greater than or equal to 0.</param>
        /// <param name="regularInMs">The waiting interval time after the second time. Must be greater than 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public static void ScheduleOnInterval(this IIntervalTimer timer, IAsyncExecutionContext fiber, Func<Task> func, int firstInMs, int regularInMs, CancellationToken token = default)
        {
            if (regularInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularInMs));
            }

            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            timer.ScheduleOnInterval(() => fiber.EnqueueTask(func), firstInMs, regularInMs, token);
        }
    }
}
