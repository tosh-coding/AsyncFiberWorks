using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using AsyncFiberWorks.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Invokes all of the subscriber's actions.
    /// </summary>
    public class ActionDriver : IActionDriver
    {
        private readonly object _lock = new object();
        private readonly LinkedList<Action<FiberExecutionEventArgs>> _actions = new LinkedList<Action<FiberExecutionEventArgs>>();
        private readonly object _lockObj = new object();
        private readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
        bool _running = false;
        IAsyncExecutor _executor;

        /// <summary>
        /// Create an action driver.
        /// </summary>
        /// <param name="executor"></param>
        public ActionDriver(IAsyncExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Create an action driver.
        /// </summary>
        /// <param name="executor"></param>
        public ActionDriver()
            : this(AsyncSimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action action)
        {
            return Subscribe((e) => action());
        }

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action<FiberExecutionEventArgs> action)
        {
            var maskableFilter = new ToggleFilter();
            Action<FiberExecutionEventArgs> safeAction = (e) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    action(e);
                }
            };

            lock (_lock)
            {
                _actions.AddLast(safeAction);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                _actions.Remove(safeAction);
            });

            return unsubscriber;
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="task">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable SubscribeAndReceiveAsTask(Func<Task> task)
        {
            return Subscribe((e) =>
            {
                e.PauseWhileRunning(task);
            });
        }

        /// <summary>
        /// Invoke all subscribers.
        /// Fibers passed as arguments will be paused.
        /// </summary>
        /// <param name="eventArgs">Handle for fiber pause.</param>
        public void InvokeAsync(FiberExecutionEventArgs eventArgs)
        {
            eventArgs.Pause();
            lock (_lock)
            {
                foreach (var action in _actions)
                {
                    Enqueue(eventArgs.SourceThread, action);
                }
            }
            Enqueue(() => eventArgs.Resume());
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers
        {
            get
            {
                lock (_lock)
                {
                    return _actions.Count;
                }
            }
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
        void Enqueue(Action action)
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
        /// <param name="threadPool">The execution context for the specified action.</param>
        /// <param name="action">Action to be executed.</param>
        void Enqueue(IThreadPool threadPool, Action<FiberExecutionEventArgs> action)
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
