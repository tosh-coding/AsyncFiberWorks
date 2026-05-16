using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Extension methods for IExecutionContext.
    /// </summary>
    public static class IExecutionContextExtensions
    {
        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="fiber">Target fiber.</param>
        /// <param name="action">Action to be executed.</param>
        public static void Enqueue(this IExecutionContext fiber, Action action)
        {
            fiber.Enqueue(ExecuteAction, action);
        }

        static void ExecuteAction(object state)
        {
            ((Action)state)?.Invoke();
        }
    }
}
