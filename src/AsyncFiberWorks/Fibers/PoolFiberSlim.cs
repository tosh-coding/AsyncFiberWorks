using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public sealed class PoolFiberSlim : IAsyncExecutionContext
    {
        private readonly object _lock = new object();
        private readonly IThreadPool _pool;
        private readonly IExecutor _executor;

        private Queue<Action> _queue = new Queue<Action>();
        private Queue<Action> _toPass = new Queue<Action>();

        private bool _flushPending;
        private bool _paused;
        private bool _flushPaused;
        private bool _resuming;

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        public PoolFiberSlim(IThreadPool pool, IExecutor executor)
        {
            _pool = pool;
            _executor = executor;
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        public PoolFiberSlim(IExecutor executor) 
            : this(DefaultThreadPool.Instance, executor)
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor.
        /// </summary>
        public PoolFiberSlim() 
            : this(DefaultThreadPool.Instance, new DefaultExecutor())
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

        private void ResumeAction(Action action)
        {
            lock (_lock)
            {
                if (_flushPaused || (!_flushPending))
                {
                    if (action != null)
                    {
                        _executor.Execute(action);
                    }
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
                    _pool.Queue((_) => ResumeAction(action));
                }
            }
        }

        /// <summary>
        /// Pauses the consumption of the task queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Pause was called twice.</exception>
        public void Pause()
        {
            lock (_lock)
            {
                if (_paused)
                {
                    throw new InvalidOperationException("Pause was called twice.");
                }
                _paused = true;
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
                if (!_paused)
                {
                    throw new InvalidOperationException("Resume was called in the unpaused state.");
                }
                if (_resuming)
                {
                    throw new InvalidOperationException("Resume was called twice.");
                }
                _resuming = true;
                _pool.Queue((_) => ResumeAction(action));
            }
        }

        /// <summary>
        /// Enqueue a single task.
        /// </summary>
        /// <param name="func">Task generator. This is done after a pause in the fiber. The generated task is monitored and takes action to resume after completion.</param>
        public void Enqueue(Func<Task<Action>> func)
        {
            this.Enqueue(() =>
            {
                this.Pause();
                Task.Run(async () =>
                {
                    Action resumingAction = default;
                    try
                    {
                        resumingAction = await func.Invoke();
                    }
                    finally
                    {
                        this.Resume(resumingAction);
                    }
                });
            });
        }
    }
}
