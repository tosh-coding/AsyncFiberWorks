using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Fiber execution notification handler arguments.
    /// </summary>
    public class FiberExecutionEventArgs : EventArgs, IFiberExecutionEventArgs
    {
        private Action _pause;
        private Action _resume;
        private IThreadPool _threadPool;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pause"></param>
        /// <param name="resume"></param>
        /// <param name="threadPool">The threads on the back side of the fiber.</param>
        public FiberExecutionEventArgs(Action pause, Action resume, IThreadPool threadPool)
        {
            _pause = pause;
            _resume = resume;
            _threadPool = threadPool;
        }

        /// <summary>
        /// Pauses the consumption of the task queue.
        /// This is only called during an Execute in the fiber.
        /// </summary>
        public void Pause()
        {
            _pause();
        }

        /// <summary>
        /// Enqueue to the threads on the back side of the fiber.
        /// </summary>
        /// <param name="action">Enqueued action.</param>
        public void EnqueueToOriginThread(Action action)
        {
            _threadPool.Queue((state) => action());
        }

        /// <summary>
        /// Resumes consumption of a paused task queue.
        /// </summary>
        public void Resume()
        {
            _resume();
        }
    }
}
