using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Extension methods for IAsyncExecutionContext.
    /// </summary>
    public static class IAsyncExecutionContextExtensions
    {
        private static void InvokeWithEventArgs(IFiberExecutionEventArgs args, object state)
        {
            ((Action<IFiberExecutionEventArgs>)state)?.Invoke(args);
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="fiber">Target fiber.</param>
        /// <param name="action">Action to be executed.</param>
        public static void Enqueue(this IAsyncExecutionContext fiber, Action<IFiberExecutionEventArgs> action)
        {
            fiber.Enqueue(InvokeWithEventArgs, action);
        }
    }
}
