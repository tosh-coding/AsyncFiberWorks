using AsyncFiberWorks.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Extension of thread pool.
    /// </summary>
    public static class ThreadPoolExtensions
    {
        /// <summary>
        /// Enqueue an action. Then returns a task to wait for the completion of the action.
        /// </summary>
        /// <param name="threadPool">A thread pool.</param>
        /// <param name="callback"></param>
        /// <returns>A task that waits until a given action is finished.</returns>
        public static async Task QueueAsync(this IThreadPool threadPool, WaitCallback callback)
        {
            var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            threadPool.Queue((_) =>
            {
                try
                {
                    callback(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    return;
                }
                tcs.SetResult(0);
            });
            await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Enqueue an action. Then returns a task to wait for the completion of the action.
        /// </summary>
        /// <param name="threadPool">A thread pool.</param>
        /// <param name="action">An action.</param>
        /// <returns>A task that waits until a given action is finished.</returns>
        public static async Task QueueAsync(this IThreadPool threadPool, Action action)
        {
            await threadPool.QueueAsync((_) => action()).ConfigureAwait(false);
        }
    }
}
