using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Fiber execution notification handler arguments.
    /// </summary>
    public class FiberExecutionEventArgs : EventArgs
    {
        private Action _pause;
        private Action<Action> _resume;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pause"></param>
        /// <param name="resume"></param>
        public FiberExecutionEventArgs(Action pause, Action<Action> resume)
        {
            _pause = pause;
            _resume = resume;
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
        /// Resumes consumption of a paused task queue.
        /// </summary>
        /// <param name="action">The action to be taken immediately after the resume.</param>
        public void Resume(Action action)
        {
            _resume(action);
        }

        /// <summary>
        /// Pause the fiber while the task is running.
        /// </summary>
        /// <param name="runningTask"></param>
        public void PauseWhileRunning(Func<Task<Action>> runningTask)
        {
            this.Pause();
            Task.Run(async () =>
            {
                Action resumingAction = default;
                try
                {
                    resumingAction = await runningTask.Invoke();
                }
                finally
                {
                    this.Resume(resumingAction);
                }
            });
        }
    }
}
