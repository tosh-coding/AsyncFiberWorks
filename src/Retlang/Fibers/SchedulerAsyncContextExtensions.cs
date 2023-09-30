using System;
using System.Threading;
using System.Threading.Tasks;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Methods for scheduling actions that will be executed in the future.
    /// </summary>
    public static class SchedulerAsyncContextExtensions
    {
        /// <summary>
        /// Schedules an action to be executed once.
        /// After await, the program resumes in the .NET ThreadPool.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task ScheduleAsync(this IExecutionContext fiber, Action action, int firstInMs, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(firstInMs, cancellationToken);
                await fiber.SwitchTo();
                action();
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                // To .NET thread pool.
                await Task.Yield();
            }
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// After await, the program resumes in the .NET ThreadPool.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <param name="cancellationToken"></param>
        public static async Task ScheduleOnIntervalAsync(this IExecutionContext fiber, Action action, int firstInMs, int regularInMs, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(firstInMs, cancellationToken).ConfigureAwait(false);
                await fiber.SwitchTo();
                action();
                while (true)
                {
                    await Task.Delay(regularInMs, cancellationToken).ConfigureAwait(false);
                    await fiber.SwitchTo();
                    action();
                }
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                // To .NET thread pool.
                await Task.Yield();
            }
        }
    }
}
