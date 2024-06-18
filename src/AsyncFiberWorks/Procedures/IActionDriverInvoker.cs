using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Action driver invoking interface.
    /// </summary>
    public interface IActionDriverInvoker
    {
        /// <summary>
        /// Invoke all subscribers.
        /// The fiber passed in the argument may be paused.
        /// </summary>
        /// <param name="eventArgs">Handle for fiber pause.</param>
        void InvokeAsync(FiberExecutionEventArgs eventArgs);
    }
}
