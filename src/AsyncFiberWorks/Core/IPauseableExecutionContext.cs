using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Sequential executor of actions. Pause is also possible.
    /// </summary>
    public interface IPauseableExecutionContext : IExecutionContext
    {
        /// <summary>
        /// Pauses the consumption of the task queue.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes consumption of a paused task queue.
        /// </summary>
        /// <param name="action">The action to be taken immediately after the resume.</param>
        void Resume(Action action);
    }
}
