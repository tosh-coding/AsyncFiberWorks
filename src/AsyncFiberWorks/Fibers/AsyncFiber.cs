﻿using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using AsyncFiberWorks.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by asynchronous control flow.
    /// </summary>
    public class AsyncFiber : IFiber
    {
        readonly object _lockObj = new object();
        readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
        bool _running = false;
        IAsyncExecutor _executor;

        /// <summary>
        /// Create a fiber.
        /// </summary>
        /// <param name="executor"></param>
        public AsyncFiber(IAsyncExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Create a fiber.
        /// </summary>
        public AsyncFiber()
            : this(AsyncSimpleExecutor.Instance)
        {
        }

        bool TryDequeue(out Func<Task> result)
        {
            lock (_lockObj)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.Dequeue();
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Start consumption.
        /// </summary>
        async void Run()
        {
            while (true)
            {
                while (TryDequeue(out var func))
                {
                    await _executor.Execute(func).ConfigureAwait(false);
                }
                lock (_lockObj)
                {
                    if (_queue.Count <= 0)
                    {
                        _running = false;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        void PrivateEnqueue(Func<Task> func)
        {
            bool startedNow = false;
            lock (_lockObj)
            {
                _queue.Enqueue(func);
                if (!_running)
                {
                    _running = true;
                    startedNow = true;
                }
            }

            if (startedNow)
            {
                Run();
            }
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action action)
        {
            PrivateEnqueue(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action<FiberExecutionEventArgs> action)
        {
            PrivateEnqueue(async () =>
            {
                bool isPaused = false;
                var tcs = new TaskCompletionSource<int>();
                var eventArgs = new FiberExecutionEventArgs(
                    () =>
                    {
                        isPaused = true;
                    },
                    () =>
                    {
                        tcs.SetResult(0);
                    },
                    DefaultThreadPool.Instance);
                action(eventArgs);
                if (isPaused)
                {
                    await tcs.Task.ConfigureAwait(false);
                    await Task.Yield();
                }
            });
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="threadPool">The execution context for the specified action.</param>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(IThreadPool threadPool, Action action)
        {
            PrivateEnqueue(async () =>
            {
                await threadPool.SwitchTo();
                action();
            });
        }

        /// <summary>
        /// Enqueue a single action. It is executed sequentially.
        /// </summary>
        /// <param name="threadPool">The execution context for the specified action.</param>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(IThreadPool threadPool, Action<FiberExecutionEventArgs> action)
        {
            PrivateEnqueue(async () =>
            {
                await threadPool.SwitchTo();
                bool isPaused = false;
                var tcs = new TaskCompletionSource<int>();
                var eventArgs = new FiberExecutionEventArgs(
                    () =>
                    {
                        isPaused = true;
                    },
                    () =>
                    {
                        tcs.SetResult(0);
                    },
                    DefaultThreadPool.Instance);
                action(eventArgs);
                if (isPaused)
                {
                    await tcs.Task.ConfigureAwait(false);
                    await Task.Yield();
                }
            });
        }
    }
}
