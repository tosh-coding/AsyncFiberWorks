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
        private IActionExceptionHandler _exceptionHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pause"></param>
        /// <param name="resume"></param>
        /// <param name="threadPool">The threads on the back side of the fiber.</param>
        /// <param name="exceptionHandler">Handler used to report or process exceptions that occur during fiber execution.</param>
        public FiberExecutionEventArgs(Action pause, Action resume, IThreadPool threadPool, IActionExceptionHandler exceptionHandler)
        {
            _pause = pause;
            _resume = resume;
            _threadPool = threadPool;
            _exceptionHandler = exceptionHandler ?? DefaultActionExceptionHandler.Instance;
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

        /// <summary>
        /// Notify the configured exception handler about an exception that occurred during fiber execution.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        public void NotifyException(Exception exception)
        {
            try
            {
                _exceptionHandler?.Handle(exception);
            }
            catch
            {
            }
        }
    }
}
