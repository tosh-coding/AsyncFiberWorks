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
        /// <param name="fiber">An fiber.</param>
        /// <param name="func">A function that returns a task.</param>
        /// <returns>A task that waits until a given task is finished.</returns>
        public static async Task EnqueueAsync(this IAsyncFiber fiber, Func<Task> func)
        {
            var tcs = new TaskCompletionSource<byte>();
            fiber.Enqueue(async () =>
            {
                try
                {
                    await func().ConfigureAwait(false);
                }
                finally
                {
                    tcs.SetResult(0);
                }
            });
            await tcs.Task;
        }
    }
}
