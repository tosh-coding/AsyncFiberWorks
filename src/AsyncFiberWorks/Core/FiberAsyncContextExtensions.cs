using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Extension of fiber asynchronous context.
    /// </summary>
    public static class FiberAsyncContextExtensions
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
            var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        /// <summary>
        /// Pause the fiber while the task is running.
        /// </summary>
        /// <param name="e">Fiber pause operation interface.</param>
        /// <param name="runningTask">A function that returns a task.</param>
        public static void PauseWhileRunning(this IFiberExecutionEventArgs e, Func<Task> runningTask)
        {
            e.Pause();
            e.EnqueueToOriginThread(async () =>
            {
                try
                {
                    await runningTask.Invoke().ConfigureAwait(false);
                }
                finally
                {
                    e.Resume();
                }
            });
        }

        /// <summary>
        /// Enqueue to the threads on the back side of the fiber.
        /// </summary>
        /// <param name="e">Fiber pause operation interface.</param>
        /// <param name="action">Enqueued action.</param>
        /// <returns>A task that waits for enqueued actions to complete.</returns>
        public static Task EnqueueToOriginThreadAsync(this IFiberExecutionEventArgs e, Action action)
        {
            var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            e.EnqueueToOriginThread(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    tcs.TrySetResult(0);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Enqueue an action. Then returns a task to wait for the completion of the action.
        /// </summary>
        /// <param name="fiber">A fiber.</param>
        /// <param name="action">An action.</param>
        /// <returns>A task that waits until a given action is finished.</returns>
        public static async Task EnqueueAsync(this IExecutionContext fiber, Action action)
        {
            var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            fiber.Enqueue(() =>
            {
                try
                {
                    action();
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
    }
}
