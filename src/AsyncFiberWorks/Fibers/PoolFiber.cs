using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public sealed class PoolFiber : IFiber
    {
        private readonly object _lock = new object();
        private readonly IThreadPool _pool;
        private readonly IExecutor _executor;
        private readonly FiberExecutionEventArgs _eventArgs;

        private Queue<Action> _queue = new Queue<Action>();
        private Queue<Action> _toPass = new Queue<Action>();

        private bool _flushPending;
        private bool _enabledPause;
        private bool _paused;
        private bool _flushPaused;
        private bool _resuming;

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        public PoolFiber(IThreadPool pool, IExecutor executor)
        {
            _pool = pool;
            _executor = executor;
            _eventArgs = new FiberExecutionEventArgs(this.Pause, this.Resume, _pool);
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        public PoolFiber(IExecutor executor) 
            : this(DefaultThreadPool.Instance, executor)
        {
        }

        /// <summary>
        /// Create a pool fiber with the specified thread pool and a simple executor.
        /// </summary>
        /// <param name="pool"></param>
        public PoolFiber(IThreadPool pool)
            : this(pool, SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and a simple executor.
        /// </summary>
        public PoolFiber()
            : this(DefaultThreadPool.Instance, SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                _queue.Enqueue(action);
                if (!_flushPending)
                {
                    _pool.Queue(Flush);
                    _flushPending = true;
                }
            }
        }

        private void Flush(object state)
        {
            var toExecute = ClearActions();
            if (toExecute != null)
            {
                while (toExecute.Count > 0)
                {
                    lock (_lock)
                    {
                        if (_paused)
                        {
                            break;
                        }
                    }
                    Action action = toExecute.Dequeue();
                    _executor.Execute(action);
                }
                lock (_lock)
                {
                    if (_paused)
                    {
                        _flushPaused = true;
                        return;
                    }
                    else if (toExecute.Count > 0)
                    {
                        // don't monopolize thread.
                        _pool.Queue(Flush);
                    }
                    else if (_queue.Count > 0)
                    {
                        // don't monopolize thread.
                        _pool.Queue(Flush);
                    }
                    else
                    {
                        _flushPending = false;
                    }
                }
            }
        }

        private Queue<Action> ClearActions()
        {
            lock (_lock)
            {
                if (_toPass.Count > 0)
                {
                    return _toPass;
                }
                if (_queue.Count == 0)
                {
                    _flushPending = false;
                    return null;
                }
                QueueUtil.Swap(ref _queue, ref _toPass);
                return _toPass;
            }
        }

        private void ResumeAction()
        {
            lock (_lock)
            {
                if (_flushPaused || (!_flushPending))
                {
                    _paused = false;
                    _flushPaused = false;
                    _resuming = false;

                    if (_flushPending)
                    {
                        _pool.Queue(Flush);
                    }
                }
                else
                {
                    // Wait flushPaused.
                    _pool.Queue((_) => ResumeAction());
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
                if (_paused)
                {
                    throw new InvalidOperationException("Pause was called twice.");
                }
                if (!_enabledPause)
                {
                    throw new InvalidOperationException("Pause is only possible within the execution context.");
                }
                _paused = true;
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
                if (!_paused)
                {
                    throw new InvalidOperationException("Resume was called in the unpaused state.");
                }
                if (_resuming)
                {
                    throw new InvalidOperationException("Resume was called twice.");
                }
                _resuming = true;
                _pool.Queue((_) => ResumeAction());
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
