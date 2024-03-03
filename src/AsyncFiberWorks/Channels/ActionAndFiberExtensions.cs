using AsyncFiberWorks.Core;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Extensions of actions and contexts.
    /// </summary>
    public static class ActionAndFiberExtensions
    {
        /// <summary>
        /// Create an action to be executed on the specified context.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fiber">Target fiber.</param>
        /// <param name="action">Action.</param>
        /// <returns>Action with enqueue.</returns>
        public static Action<T> CreateAction<T>(this IExecutionContext fiber, Action<T> action)
        {
            if (fiber == null)
            {
                return action;
            }
            else
            {
                return (msg) => fiber.Enqueue(() => action(msg));
            }
        }

        public static Action<T> CreateAction<T>(this IPauseableExecutionContext fiber, Func<T, Task> func)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            return (msg) => fiber.Enqueue(async () =>
            {
                var task = func(msg);
                await fiber.PauseWhileRunning(async () =>
                {
                    await task;
                    return () => { };
                });
            });
        }

        public static Action<T> CreateAction<T>(this IPauseableExecutionContext fiber, Func<T, Task<Action>> func)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }
            return (msg) => fiber.Enqueue(async () =>
            {
                var task = func(msg);
                await fiber.PauseWhileRunning(task);
            });
        }

        /// <summary>
        /// Pause the fiber until the task is completed.
        /// </summary>
        /// <param name="task">Tasks to be monitored. The task should return a resume function.</param>
        /// <returns>Tasks until the fiber resumes.</returns>
        public static Task PauseWhileRunning(this IPauseableExecutionContext fiber, Task<Action> task)
        {
            fiber.Pause();
            return Task.Run(async () =>
            {
                var action = await task;
                fiber.Resume(action);
            });
        }

        /// <summary>
        /// Pause the fiber until the task is completed.
        /// </summary>
        /// <param name="func">Function to retrieve the task to be monitored. The task should return a resume function.</param>
        /// <returns>Tasks until the fiber resumes.</returns>
        public static Task PauseWhileRunning(this IPauseableExecutionContext fiber, Func<Task<Action>> func)
        {
            fiber.Pause();
            var task = func();
            return Task.Run(async () =>
            {
                var action = await task;
                fiber.Resume(action);
            });
        }
    }
}
