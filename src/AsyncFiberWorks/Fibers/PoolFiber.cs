using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
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
        private readonly IActionExceptionHandler _exceptionHandler;
        private readonly FiberExecutionEventArgs _eventArgs;

        private Queue<(Action<object>, object)> _queue;
        private Queue<(Action<object>, object)> _toPass;

        private bool _flushPending;
        private bool _enabledPause;
        private bool _paused;
        private bool _flushPaused;
        private bool _resuming;

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="exceptionHandler">An exception handler. If null, exceptions are ignored.</param>
        /// <param name="initialCapacity"></param>
        /// <exception cref="ArgumentNullException">pool must be non-null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">initialCapacity must be greater than or equal to 1.</exception>
        public PoolFiber(IThreadPool pool, IActionExceptionHandler exceptionHandler, int initialCapacity = 4)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }
            _pool = pool;
            _exceptionHandler = exceptionHandler;
            _eventArgs = new FiberExecutionEventArgs(this.Pause, this.Resume, _pool, exceptionHandler);
            _queue = new Queue<(Action<object>, object)>(initialCapacity);
            _toPass = new Queue<(Action<object>, object)>(initialCapacity);
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        /// <param name="exceptionHandler">An exception handler. If null, exceptions are ignored.</param>
        public PoolFiber(IActionExceptionHandler exceptionHandler) 
            : this(DefaultThreadPool.Instance, exceptionHandler)
        {
        }

        /// <summary>
        /// Create a pool fiber with the specified thread pool and a simple executor.
        /// </summary>
        /// <param name="pool"></param>
        public PoolFiber(IThreadPool pool)
            : this(pool, null)
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and a simple executor.
        /// </summary>
        public PoolFiber()
            : this(DefaultThreadPool.Instance, null)
        {
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        public void Enqueue(Action<object> action, object state)
        {
            lock (_lock)
            {
                _queue.Enqueue((action, state));
                if (!_flushPending)
                {
                    _pool.Queue(Flush);
                    _flushPending = true;
                }
            }
        }

        private void Flush()
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
                    var tuple = toExecute.Dequeue();
                    try
                    {
                        tuple.Item1?.Invoke(tuple.Item2);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _exceptionHandler?.Handle(ex);
                        }
                        catch { }
                    }
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

        private Queue<(Action<object>, object)> ClearActions()
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
                Swap(ref _queue, ref _toPass);
                return _toPass;
            }
        }

        private static void Swap(ref Queue<(Action<object>, object)> a, ref Queue<(Action<object>, object)> b)
        {
            var tmp = a;
            a = b;
            b = tmp;
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
                    _pool.Queue(ResumeAction);
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
                _pool.Queue(ResumeAction);
            }
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action.</param>
        public void Enqueue(Action<IFiberExecutionEventArgs, object> action, object state)
        {
            this.Enqueue(ExecuteActionWithEventArgs, (action, state));
        }

        private void ExecuteActionWithEventArgs(object state)
        {
            var tuple = ((Action<IFiberExecutionEventArgs, object>, object))state;
            lock (_lock)
            {
                _enabledPause = true;
            }
            try
            {
                tuple.Item1?.Invoke(_eventArgs, tuple.Item2);
            }
            catch (Exception ex)
            {
                try
                {
                    _exceptionHandler?.Handle(ex);
                }
                catch { }
            }
            finally
            {
                lock (_lock)
                {
                    _enabledPause = false;
                }
            }
        }
    }
}
