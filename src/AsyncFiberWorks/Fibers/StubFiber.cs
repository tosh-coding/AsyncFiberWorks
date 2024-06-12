using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

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

        private int _paused = 0;
        private Action _resumeAction = null;

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
            _eventArgs = new FiberExecutionEventArgs(this.Pause, this.Resume);
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
        public int ExecuteAll()
        {
            int count = 0;
            Action toExecute;
            while (true)
            {
                lock (_lock)
                {
                    if (_paused != 0 && _paused != 2)
                    {
                        break;
                    }
                }

                ExecuteResumeProcess();

                if (!_pending.TryDequeue(out toExecute))
                {
                    break;
                }
                _executor.Execute(toExecute);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// Execute only what is pending now.
        /// </summary>
        public int ExecuteOnlyPendingNow()
        {
            int count = _pending.Count;
            int ret = count;
            Action toExecute;
            while (true)
            {
                lock (_lock)
                {
                    if (_paused != 0 && _paused != 2)
                    {
                        break;
                    }
                }

                ExecuteResumeProcess();

                if (!_pending.TryDequeue(out toExecute))
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
            return ret;
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
                _paused = 1;
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

            _resumeAction = action;
        }

        private void ExecuteResumeProcess()
        {
            if (_paused != 2)
            {
                return;
            }

            var action = _resumeAction;
            _resumeAction = null;
            try
            {
                action?.Invoke();
            }
            finally
            {
                lock (_lock)
                {
                    _paused = 0;
                }
            }
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action<FiberExecutionEventArgs> action)
        {
            this.Enqueue(() => action(_eventArgs));
        }
    }
}
