using AsyncFiberWorks.Core;
using System;

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
    }
}
