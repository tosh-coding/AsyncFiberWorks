using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Extensions to AsyncFiber.
    /// </summary>
    public static class AsyncFiberExtentions
    {
        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="fiber">A fiber.</param>
        /// <param name="func">A function that returns a task.</param>
        public static void EnqueueTask(this IAsyncExecutionContext fiber, Func<Task> func)
        {
            fiber.Enqueue((e) => e.PauseWhileRunning(func));
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="fiber">A fiber.</param>
        /// <param name="func">A function that returns a task.</param>
        /// <returns>A task that waits until a given task is finished.</returns>
        public static async Task EnqueueTaskAsync(this IAsyncExecutionContext fiber, Func<Task> func)
        {
            var tcs = new TaskCompletionSource<byte>();
            fiber.Enqueue((e) => e.PauseWhileRunning(async () =>
            {
                try
                {
                    await func().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    return;
                }
                tcs.SetResult(0);
            }));
            await tcs.Task.ConfigureAwait(false);
        }
    }
}
