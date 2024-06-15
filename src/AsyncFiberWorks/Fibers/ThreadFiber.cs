using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : IFiber, IDisposable
    {
        private readonly object _lock = new object();
        private readonly IDedicatedConsumerThreadWork _queue;
        private readonly UserWorkerThread _workerThread;
        private readonly FiberExecutionEventArgs _eventArgs;
        private bool _stopped = false;
        private bool _paused;
        private bool _resuming;
        private AutoResetEvent _autoReset = null;
        private Action _resumeAction = null;

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiber()
            : this(new DefaultQueue())
        {
        }

        /// <summary>
        /// Create a thread fiber with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IDedicatedConsumerThreadWork queue)
            : this(queue, null)
        {
        }

        /// <summary>
        /// Create a thread fiber with the specified thread name.
        /// </summary>
        /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
            : this(new DefaultQueue(), threadName)
        {
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IDedicatedConsumerThreadWork queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _queue = queue;
            _eventArgs = new FiberExecutionEventArgs(this.Pause, this.Resume);
            _workerThread = new UserWorkerThread(queue, threadName, isBackground, priority);
            _workerThread.Start();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Stop()
        {
            if (!_stopped)
            {
                _workerThread.Stop();
                _stopped = true;
            }
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Pauses the consumption of the task queue.
        /// This is only called during an Execute in the fiber.
        /// </summary>
        /// <exception cref="InvalidOperationException">Pause was called twice.</exception>
        private void Pause()
        {
            lock (_lock)
            {
                if (_paused)
                {
                    throw new InvalidOperationException("Pause was called twice.");
                }
                _paused = true;
                if (_autoReset == null)
                {
                    _autoReset = new AutoResetEvent(false);
                }
            }
        }

        /// <summary>
        /// Resumes consumption of a paused task queue.
        /// </summary>
        /// <param name="action">The action to be taken immediately after the resume.</param>
        /// <exception cref="InvalidOperationException">Resume was called in the unpaused state.</exception>
        private void Resume(Action action)
        {
            lock (_lock)
            {
                if (!_paused)
                {
                    throw new InvalidOperationException("Resume was called in the unpaused state.");
                }
                if (_resuming)
                {
                    throw new InvalidOperationException("Resume was called twice.");
                }
                _resuming = true;
                _resumeAction = action;
                _autoReset.Set();
            }
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action<FiberExecutionEventArgs> action)
        {
            this.Enqueue(() =>
            {
                action(_eventArgs);
                bool tmpPaused;
                lock (_lock)
                {
                    tmpPaused = _paused;
                }
                if (tmpPaused)
                {
                    _autoReset.WaitOne();
                    try
                    {
                        _resumeAction();
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _paused = false;
                            _resuming = false;
                            _resumeAction = null;
                        }
                    }
                }
            });
        }
    }
}
