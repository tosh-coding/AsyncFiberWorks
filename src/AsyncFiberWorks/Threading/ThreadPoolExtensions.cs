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
        /// <param name="callback">The callback method to be executed by a thread pool thread.</param>
        /// <param name="state">An object containing information to be used by the callback method.</param>
        /// <returns>A task that waits until a given action is finished.</returns>
        public static async Task QueueAsync(this IThreadPool threadPool, WaitCallback callback, object state = null)
        {
            var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            threadPool.Queue((x) =>
            {
                try
                {
                    callback(x);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    return;
                }
                tcs.SetResult(0);
            }, state);
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
            await threadPool.QueueAsync((x) => ((Action)x)?.Invoke(), action).ConfigureAwait(false);
        }

        /// <summary>
        /// Wait for WaitHandle.
        /// </summary>
        /// <param name="threadPool">Thread pool used for waiting.</param>
        /// <param name="waitHandle">Waiting target.</param>
        /// <param name="cancellationToken">Handle for cancellation.</param>
        /// <returns>Task waiting for WaitHandle to complete.</returns>
        /// <exception cref="OperationCanceledException">An exception that occurs when canceled.</exception>
        public static async Task RegisterWaitForSingleObjectAsync(this IThreadPool threadPool, WaitHandle waitHandle, CancellationToken cancellationToken)
        {
            await threadPool.QueueAsync(() =>
            {
                int index = WaitHandle.WaitAny(new WaitHandle[]
                {
                    waitHandle,
                    cancellationToken.WaitHandle
                });
                if (index == 0)
                {
                    // completed.
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }).ConfigureAwait(false);
        }
    }
}
