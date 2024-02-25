using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// An action with an execution context.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionWithContext<T>
    {
        /// <summary>
        /// Action.
        /// </summary>
        private readonly Action<T> _action;

        /// <summary>
        /// The context in which the action should be executed.
        /// </summary>
        private readonly IExecutionContext _fiber;

        /// <summary>
        /// Create a pair of Action and Fiber.
        /// </summary>
        /// <param name="action">Message receive handler.</param>
        /// <param name="fiber">The context in which to execute.</param>
        public ActionWithContext(Action<T> action, IExecutionContext fiber)
        {
            _action = action;
            _fiber = fiber;
        }

        /// <summary>
        /// Message receive handler. It is executed on fibers.
        /// </summary>
        /// <param name="msg">A message.</param>
        public void OnReceive(T msg)
        {
            _fiber.Enqueue(() => _action(msg));
        }
    }
}
