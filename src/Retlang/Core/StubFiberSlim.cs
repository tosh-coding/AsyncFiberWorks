using System;
using System.Collections.Concurrent;

namespace Retlang.Core
{
    /// <summary>
    /// This is a fiber that needs to be pumped manually.
    /// Queued actions are added to the pending list.
    /// Consume them by periodically calling methods for execution.
    /// Periodically call a method for execution. They are executed on their calling thread.
    /// </summary>
    public sealed class StubFiberSlim : IExecutionContext
    {
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private readonly IExecutor _executor;

        private int _paused = 0;

        /// <summary>
        /// Create a stub fiber with the default executor.
        /// </summary>
        public StubFiberSlim()
            : this(new DefaultExecutor())
        {
        }

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiberSlim(IExecutor executor)
        {
            _executor = executor;
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
                    if (_paused != 0)
                    {
                        break;
                    }
                }

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
                    if (_paused != 0)
                    {
                        break;
                    }
                }

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
        /// </summary>
        /// <exception cref="InvalidOperationException">Pause was called twice.</exception>
        public void Pause()
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
        public void Resume(Action action)
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

            try
            {
                action();
            }
            finally
            {
                lock (_lock)
                {
                    _paused = 0;
                }
            }
        }
    }
}
