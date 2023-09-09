using System;
using System.Threading;
using System.Threading.Tasks;

namespace Retlang.Fibers
{
    public static class SchedulerForIFiberSlimExtensions
    {
        /// <summary>
        /// Schedules an action to be executed once.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task Schedule(IFiberSlim fiber, Action action, int firstInMs, CancellationToken cancellationToken)
        {
            await Task.Delay(firstInMs, cancellationToken);
            await fiber.SwitchTo();
            action();
            await Task.Yield();
        }

        /// <summary>
        /// Schedule an action to be executed on a recurring interval.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        public static async Task ScheduleOnInterval(IFiberSlim fiber, Action action, int firstInMs, int regularInMs, CancellationToken cancellationToken)
        {
            await Task.Delay(firstInMs, cancellationToken).ConfigureAwait(false);
            await fiber.SwitchTo();
            action();
            try
            {
                while (true)
                {
                    await Task.Delay(regularInMs, cancellationToken).ConfigureAwait(false);
                    await fiber.SwitchTo();
                    action();
                }
            }
            finally
            {
                await Task.Yield();
            }
        }
    }
}
