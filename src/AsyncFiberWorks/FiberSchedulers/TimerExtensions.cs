using System;
using System.Threading;
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
        /// <param name="timer"></param>
        /// <param name="fiber">The context in which the action is executed.</param>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstInMs">Timer wait time. Must be greater than or equal to 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public static void Schedule(this IOneshotTimer timer, IExecutionContext fiber, Action action, int firstInMs, CancellationToken token = default)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            timer.InternalSchedule((state) => fiber.Enqueue((Action)state), action, firstInMs, token);
        }

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
        /// Schedules a task to be executed once.
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="fiber">The context in which the action is executed.</param>
        /// <param name="func">Task generator.</param>
        /// <param name="firstInMs">Timer wait time. Must be greater than or equal to 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public static void Schedule(this IOneshotTimer timer, IAsyncExecutionContext fiber, Func<Task> func, int firstInMs, CancellationToken token = default)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            timer.InternalSchedule((state) => fiber.EnqueueTask((Func<Task>)state), func, firstInMs, token);
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

        /// <summary>
        /// Generate a wait time task.
        /// </summary>
        /// <param name="timer">A timer.</param>
        /// <param name="millisecondsDelay">Wait time.</param>
        /// <param name="token"></param>
        /// <returns>A task that is completed after a specified amount of time.</returns>
        public static Task ScheduleAsync(this IOneshotTimer timer, int millisecondsDelay, CancellationToken token = default)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            timer.InternalSchedule((state) =>
            {
                ((TaskCompletionSource<int>)state).SetResult(0);
            }, tcs, millisecondsDelay, token);
            return tcs.Task;
        }
    }
}
