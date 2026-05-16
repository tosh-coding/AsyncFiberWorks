using System;
using System.Collections.Generic;
using System.Threading;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Manages a set of <see cref="PoolFiber"/> instances keyed by an integer.
    /// Each key owns a fiber that serializes actions submitted for that key.
    /// Fibers are created on demand and removed when no pending actions remain.
    /// </summary>
    public class KeyedPoolFiber
    {
        private readonly object _lock = new object();
        private readonly IThreadPool _pool;
        private readonly IActionExceptionHandler _exceptionHandler;
        private readonly Dictionary<int, FiberEntry> _fibers = new Dictionary<int, FiberEntry>();
        private readonly int _cacheCount;
        private readonly Stack<FiberEntry> _cachedFiberEntries = new Stack<FiberEntry>();

        private class FiberEntry
        {
            /// <summary>
            /// The underlying PoolFiber that executes actions serially for this key.
            /// </summary>
            public PoolFiber Fiber;

            /// <summary>
            /// Number of outstanding enqueued actions (or references) for this fiber.
            /// </summary>
            public int Count;
        }

        /// <summary>
        /// Wraps <see cref="IFiberExecutionEventArgs"/> and delays completion callback
        /// until Resume when Pause was invoked.
        /// </summary>
        private sealed class CompletionAwareFiberExecutionEventArgs : IFiberExecutionEventArgs
        {
            private readonly IFiberExecutionEventArgs _inner;
            private readonly Action _onCompleted;
            private int _paused;
            private int _completed;

            public CompletionAwareFiberExecutionEventArgs(IFiberExecutionEventArgs inner, Action onCompleted)
            {
                _inner = inner;
                _onCompleted = onCompleted;
            }

            public void Pause()
            {
                _inner.Pause();
                Volatile.Write(ref _paused, 1);
            }

            public void Resume()
            {
                _inner.Resume();
                CompleteOnce();
            }

            public void EnqueueToOriginThread(Action action)
            {
                _inner.EnqueueToOriginThread(action);
            }

            public void CompleteIfNotPaused()
            {
                if (Volatile.Read(ref _paused) == 0)
                {
                    CompleteOnce();
                }
            }

            private void CompleteOnce()
            {
                if (Interlocked.Exchange(ref _completed, 1) == 0)
                {
                    _onCompleted();
                }
            }

            /// <summary>
            /// Notify the configured exception handler about an exception that occurred during fiber execution.
            /// </summary>
            /// <param name="exception">The exception to report.</param>
            public void NotifyException(Exception exception)
            {
                _inner.NotifyException(exception);
            }
        }

        /// <summary>
        /// Constructs a keyed fiber manager that uses the specified thread pool.
        /// </summary>
        /// <param name="pool">Thread pool instance used to schedule fiber work. Must not be null.</param>
        /// <param name="exceptionHandler">An exception handler used by created PoolFiber instances. If null, exceptions are ignored.</param>
        /// <param name="cacheCount">Maximum number of idle fiber entries to cache for reuse.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pool"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cacheCount"/> is less than 0.</exception>
        public KeyedPoolFiber(IThreadPool pool, IActionExceptionHandler exceptionHandler = null, int cacheCount = 0)
        {
            if (cacheCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cacheCount));
            }

            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _exceptionHandler = exceptionHandler;
            _cacheCount = cacheCount;
        }

        /// <summary>
        /// Convenience constructor that uses the default thread pool.
        /// </summary>
        public KeyedPoolFiber()
            : this(DefaultThreadPool.Instance, null, 0)
        {
        }

        /// <summary>
        /// Convenience constructor that uses the default thread pool and sets cache size.
        /// </summary>
        /// <param name="cacheCount">Maximum number of idle fiber entries to cache for reuse.</param>
        public KeyedPoolFiber(int cacheCount)
            : this(DefaultThreadPool.Instance, null, cacheCount)
        {
        }

        /// <summary>
        /// Returns the existing fiber for <paramref name="key"/> or creates a new one.
        /// Also increments the entry reference count so the fiber remains tracked until work completes.
        /// </summary>
        private PoolFiber GetOrCreateFiber(int key)
        {
            lock (_lock)
            {
                if (!_fibers.TryGetValue(key, out var entry))
                {
                    if (_cachedFiberEntries.Count > 0)
                    {
                        entry = _cachedFiberEntries.Pop();
                        entry.Count = 0;
                    }
                    else
                    {
                        entry = new FiberEntry
                        {
                            Fiber = new PoolFiber(_pool, _exceptionHandler),
                            Count = 0,
                        };
                    }

                    _fibers[key] = entry;
                }
                entry.Count += 1;
                return entry.Fiber;
            }
        }

        /// <summary>
        /// Decrements the reference count for the fiber associated with <paramref name="key"/>.
        /// If the count reaches zero the entry is removed from the dictionary.
        /// Removed entries are cached up to cache capacity for reuse.
        /// </summary>
        private void DecrementCount(int key)
        {
            lock (_lock)
            {
                if (!_fibers.TryGetValue(key, out var entry))
                {
                    return;
                }

                entry.Count -= 1;
                if (entry.Count <= 0)
                {
                    _fibers.Remove(key);

                    if (_cacheCount > 0 && _cachedFiberEntries.Count < _cacheCount)
                    {
                        _cachedFiberEntries.Push(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Enqueues an action to the fiber associated with <paramref name="key"/>.
        /// The fiber will execute actions serially for that key.
        /// </summary>
        /// <param name="key">Key identifying the fiber.</param>
        /// <param name="action">Action to execute on the keyed fiber.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        public void EnqueueKeyed(int key, Action<object> action, object state)
        {
            var fiber = GetOrCreateFiber(key);
            fiber.Enqueue(() =>
            {
                try
                {
                    action?.Invoke(state);
                }
                finally
                {
                    DecrementCount(key);
                }
            });
        }

        /// <summary>
        /// Enqueues an action to the fiber associated with <paramref name="key"/>.
        /// The fiber will execute actions serially for that key.
        /// </summary>
        /// <param name="key">Key identifying the fiber.</param>
        /// <param name="action">Action to execute on the keyed fiber.</param>
        public void EnqueueKeyed(int key, Action action)
        {
            this.EnqueueKeyed(key, ExecuteAction, action);
        }

        static void ExecuteAction(object state)
        {
            ((Action)state)?.Invoke();
        }

        /// <summary>
        /// Enqueues an action that receives fiber execution event args to the fiber associated with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key identifying the fiber.</param>
        /// <param name="action">Action that accepts <see cref="IFiberExecutionEventArgs"/>.</param>
        public void EnqueueKeyed(int key, Action<IFiberExecutionEventArgs> action)
        {
            var fiber = GetOrCreateFiber(key);
            fiber.Enqueue((e) =>
            {
                var wrapped = new CompletionAwareFiberExecutionEventArgs(e, () => DecrementCount(key));
                try
                {
                    action(wrapped);
                }
                finally
                {
                    wrapped.CompleteIfNotPaused();
                }
            });
        }

        /// <summary>
        /// Creates a proxy <see cref="IFiber"/> that targets the fiber associated with <paramref name="key"/>.
        /// Useful when callers want an IFiber instance to schedule work against the keyed fiber.
        /// </summary>
        /// <param name="key">Key identifying the fiber.</param>
        /// <returns>An IFiber proxy that enqueues work on the keyed fiber.</returns>
        public IFiber CreateFiber(int key)
        {
            return new KeyedPoolFiberProxy(key, this);
        }
    }
}
