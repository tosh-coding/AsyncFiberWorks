using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Fiber pause operation interface.
    /// </summary>
    public interface IFiberExecutionEventArgs
    {
        /// <summary>
        /// Pauses the consumption of the task queue.
        /// This is only called during an Execute in the fiber.
        /// </summary>
        void Pause();

        /// <summary>
        /// Enqueue to the threads on the back side of the fiber.
        /// </summary>
        /// <param name="action">Enqueued action.</param>
        void EnqueueToOriginThread(Action action);

        /// <summary>
        /// Resumes consumption of a paused task queue.
        /// </summary>
        void Resume();

        /// <summary>
        /// Notify the configured exception handler about an exception that occurred during fiber execution.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        void NotifyException(Exception exception);
    }
}
