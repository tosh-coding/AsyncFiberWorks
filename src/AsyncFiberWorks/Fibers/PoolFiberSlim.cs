using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public sealed class PoolFiberSlim : IExecutionContext
    {
        private readonly object _lock = new object();
        private readonly IThreadPool _pool;
        private readonly IExecutor _executor;

        private Queue<Action> _queue = new Queue<Action>();
        private Queue<Action> _toPass = new Queue<Action>();

        private bool _flushPending;
        private int _paused = 0;

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
                    Action action = toExecute.Dequeue();
                    _executor.Execute(action);

                    lock (_lock)
                    {
                        if (_paused != 0)
                        {
                            return;
                        }
                    }
                }
                lock (_lock)
                {
                    if (_queue.Count > 0)
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
                if (_paused != 0)
                {
                    return null;
                }
                if (_toPass.Count > 0)
                {
                    return _toPass;
                }
                if (_queue.Count == 0)
                {
                    _flushPending = false;
                    return null;
                }
                Queues.Swap(ref _queue, ref _toPass);
                return _toPass;
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
                    if (_flushPending)
                    {
                        _pool.Queue(Flush);
                    }
                }
            }
        }

        /// <summary>
        /// Pause the fiber until the task is completed.
        /// </summary>
        /// <param name="task">Tasks to be monitored.</param>
        /// <returns>Tasks until the fiber resumes.</returns>
        public Task PauseWhileRunning(Task task)
        {
            Pause();
            return Task.Run(async () =>
            {
                try
                {
                    await task;
                }
                finally
                {
                    Resume(() => { });
                }
            });
        }

        /// <summary>
        /// Pause the fiber until the task is completed.
        /// </summary>
        /// <param name="func">Function to retrieve the task to be monitored.</param>
        /// <returns>Tasks until the fiber resumes.</returns>
        public Task PauseWhileRunning(Func<Task> func)
        {
            Pause();
            var task = func();
            return Task.Run(async () =>
            {
                try
                {
                    await task;
                }
                finally
                {
                    Resume(() => { });
                }
            });
        }
    }
}
