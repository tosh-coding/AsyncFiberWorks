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
    }
}
