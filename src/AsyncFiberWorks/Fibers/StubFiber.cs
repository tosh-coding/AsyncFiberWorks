using System;
using System.Collections.Concurrent;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// This is a fiber that needs to be pumped manually.
    /// Queued actions are added to the pending list.
    /// Consume them by periodically calling methods for execution.
    /// Periodically call a method for execution. They are executed on their calling thread.
    /// </summary>
    public sealed class StubFiber : IFiber
    {
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private readonly IExecutor _executor;
        private readonly FiberExecutionEventArgs _eventArgs;
        private readonly ThreadPoolAdaptor _queueUsedDuringPause;

        private bool _enabledPause;
        private int _paused = 0;

        /// <summary>
        /// Create a stub fiber with a simple executor.
        /// </summary>
        public StubFiber()
            : this(SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiber(IExecutor executor)
        {
            _executor = executor;
            _queueUsedDuringPause = new ThreadPoolAdaptor(new DefaultQueue());
            _eventArgs = new FiberExecutionEventArgs(this.Pause, this.Resume, _queueUsedDuringPause);
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _pending.Enqueue(action);
        }

        /// <summary>
        /// Execute until there are no more pending actions.
        /// </summary>
        public void ExecuteAll()
        {
            bool isPaused = ExecuteResumeProcess();
            if (isPaused)
            {
                return;
            }

            while (true)
            {
                lock (_lock)
                {
                    if (_paused != 0)
                    {
                        break;
                    }
                }

                if (!_pending.TryDequeue(out var toExecute))
                {
                    break;
                }
                _executor.Execute(toExecute);
            }
        }

        /// <summary>
        /// Execute only what is pending now.
        /// </summary>
        public void ExecuteOnlyPendingNow()
        {
            bool isPaused = ExecuteResumeProcess();
            if (isPaused)
            {
                return;
            }

            int count = _pending.Count;
            while (true)
            {
                lock (_lock)
                {
                    if (_paused != 0)
                    {
                        break;
                    }
                }

                if (!_pending.TryDequeue(out var toExecute))
                {
                    break;
                }
                _executor.Execute(toExecute);
                count -= 1;
                if (count <= 0)
                {
                    break;
                }
            }
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
                if (_paused != 0)
                {
                    throw new InvalidOperationException("Pause was called twice.");
                }
                if (!_enabledPause)
                {
                    throw new InvalidOperationException("Pause is only possible within the execution context.");
                }
                _paused = 1;
            }
        }

        /// <summary>
        /// Resumes consumption of a paused task queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Resume was called in the unpaused state.</exception>
        private void Resume()
        {
            lock (_lock)
            {
                if (_paused == 0)
                {
                    throw new InvalidOperationException("Resume was called in the unpaused state.");
                }
                if (_paused != 1)
                {
                    throw new InvalidOperationException("Resume was called twice.");
                }
                _paused = 2;
            }
        }

        private bool ExecuteResumeProcess()
        {
            lock (_lock)
            {
                if (_paused == 0)
                {
                    return false;
                }
            }

            _queueUsedDuringPause.Queue((_) =>
            {
                _queueUsedDuringPause.Stop();
            });
            _queueUsedDuringPause.Run();
            lock (_lock)
            {
                if (_paused != 2)
                {
                    return true;
                }
                _paused = 0;
                return false;
            }
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action<FiberExecutionEventArgs> action)
        {
            this.Enqueue(() =>
            {
                lock (_lock)
                {
                    _enabledPause = true;
                }
                action(_eventArgs);
                lock (_lock)
                {
                    _enabledPause = false;
                }
            });
        }
    }
}
