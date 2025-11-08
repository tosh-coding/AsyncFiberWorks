using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of destination fiber and task pairs.
    /// Call all tasks in the order in which they were registered.
    /// Can specify the fiber to be executed.
    /// Wait for the calls to complete one by one before proceeding.
    /// </summary>
    public class FiberAndTaskPairList : ISequentialTaskInvoker
    {
        private readonly object _lock = new object();
        private readonly LinkedList<RegisteredAction> _actions = new LinkedList<RegisteredAction>();
        private readonly List<RegisteredAction> _copiedActions = new List<RegisteredAction>();
        private readonly IActionExecutor _executor;
        private bool _inInvoking = false;
        private int _nextIndex = 0;
        private TaskCompletionSource<int> _tcsEnd = null;
        private IFiber _defaultContext = null;

        /// <summary>
        /// Create a list with specified executer.
        /// </summary>
        /// <param name="executor"></param>
        public FiberAndTaskPairList(IActionExecutor executor)
        {
            _executor = executor ?? SimpleExecutor.Instance;
        }

        /// <summary>
        /// Create a list.
        /// </summary>
        public FiberAndTaskPairList()
            : this(null)
        {
        }

        /// <summary>
        /// Add an action to the tail.
        /// </summary>
        /// <param name="action">Action to be performed.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Action action, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Action safeAction = () =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    action();
                }
            };
            var registeredAction = new RegisteredAction()
            {
                ActionType = ActionType.SimpleAction,
                Context = context,
                SimpleAction = safeAction,
            };

            lock (_lock)
            {
                _actions.AddLast(registeredAction);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                lock (_lock)
                {
                    _actions.Remove(registeredAction);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Add a task to the tail.
        /// </summary>
        /// <param name="task">Task to be performed.</param>
        /// <param name="context">The context in which the task will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Action<IFiberExecutionEventArgs> task, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Action<IFiberExecutionEventArgs> safeAction = (e) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    task(e);
                }
            };
            var registeredAction = new RegisteredAction()
            {
                ActionType = ActionType.ActionFiberExecutionEventArgs,
                Context = context,
                ActionFiberExecutionEventArgs = safeAction,
            };

            lock (_lock)
            {
                _actions.AddLast(registeredAction);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                lock (_lock)
                {
                    _actions.Remove(registeredAction);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Add a task to the tail.
        /// </summary>
        /// <param name="task">Task to be performed.</param>
        /// <param name="context">The context in which the task will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Func<Task> task, IFiber context = null)
        {
            return this.Add((IFiberExecutionEventArgs e) => e.PauseWhileRunning(task), context);
        }

        /// <summary>
        /// Invoke all tasks sequentially.
        /// </summary>
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        /// <returns>A task that waits for tasks to be performed.</returns>
        public async Task InvokeSequentialAsync(IFiber defaultContext)
        {
            lock (_lock)
            {
                if (_inInvoking)
                {
                    throw new InvalidOperationException("InvokeAsync can be called only once at the same time.");
                }

                _copiedActions.Clear();
                _copiedActions.AddRange(_actions);
                if (_copiedActions.Count <= 0)
                {
                    return;
                }
                _inInvoking = true;
            }

            _nextIndex = 0;
            _tcsEnd = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _defaultContext = defaultContext;

            try
            {
                enqueueNextAction();
                await _tcsEnd.Task.ConfigureAwait(false);
            }
            finally
            {
                _copiedActions.Clear();
                _tcsEnd = null;
                _defaultContext = null;
                lock (_lock)
                {
                    _inInvoking = false;
                }
                await Task.Yield();
            }
        }

        RegisteredAction getNextAction()
        {
            if (_nextIndex >= _copiedActions.Count)
            {
                return null;
            }
            var result = _copiedActions[_nextIndex];
            _nextIndex += 1;
            return result;
        }

        void enqueueNextAction()
        {
            var nextAction = getNextAction();
            if (nextAction == null)
            {
                _tcsEnd.SetResult(0);
            }
            else
            {
                var nextContext = nextAction.Context ?? _defaultContext;
                if (nextAction.ActionType == ActionType.SimpleAction)
                {
                    nextContext.Enqueue(() =>
                    {
                        try
                        {
                            _executor.Execute(nextAction.SimpleAction);
                        }
                        finally
                        {
                            enqueueNextAction();
                        }
                    });
                }
                else if (nextAction.ActionType == ActionType.ActionFiberExecutionEventArgs)
                {
                    nextContext.Enqueue((e) =>
                    {
                        _executor.Execute(e, (arg) =>
                        {
                            var eventArgs = new EnqueueNextActionEventArgs(arg, enqueueNextAction);
                            try
                            {
                                nextAction.ActionFiberExecutionEventArgs(eventArgs);
                            }
                            finally
                            {
                                eventArgs.CheckAndEnqueue();
                            }
                        });
                    });
                }
                else
                {
                    throw new Exception($"Unknown ActionType found. ActionType={nextAction.ActionType}, nextIndex={_nextIndex}.");
                }
            }
        }

        internal class RegisteredAction
        {
            public ActionType ActionType;
            public IFiber Context;
            public Action SimpleAction;
            public Action<IFiberExecutionEventArgs> ActionFiberExecutionEventArgs;
        }

        internal enum ActionType
        {
            SimpleAction,
            ActionFiberExecutionEventArgs,
        }

        /// <summary>
        /// Calling enqueueNextAction.
        /// </summary>
        internal class EnqueueNextActionEventArgs : IFiberExecutionEventArgs
        {
            private readonly object _lock = new object();
            private readonly IFiberExecutionEventArgs _originEventArgs;
            private readonly Action _enqueueNextAction;
            private bool _enqueuedNextAction = false;
            private bool _paused = false;

            /// <summary>
            /// Create a instance.
            /// </summary>
            /// <param name="originEventArgs"></param>
            /// <param name="enqueueNextAction"></param>
            public EnqueueNextActionEventArgs(IFiberExecutionEventArgs originEventArgs, Action enqueueNextAction)
            {
                _originEventArgs = originEventArgs;
                _enqueueNextAction = enqueueNextAction;
            }

            /// <summary>
            /// Pauses the consumption of the task queue.
            /// This is only called during an Execute in the fiber.
            /// </summary>
            public void Pause()
            {
                _originEventArgs.Pause();
                lock (_lock)
                {
                    if (!_enqueuedNextAction)
                    {
                        _paused = true;
                    }
                }
            }

            /// <summary>
            /// Enqueue to the threads on the back side of the fiber.
            /// </summary>
            /// <param name="action">Enqueued action.</param>
            public void EnqueueToOriginThread(Action action)
            {
                _originEventArgs.EnqueueToOriginThread(action);
            }

            /// <summary>
            /// Resumes consumption of a paused task queue.
            /// </summary>
            public void Resume()
            {
                _originEventArgs.Resume();
                bool needEnqueue = false;
                lock (_lock)
                {
                    if (!_enqueuedNextAction)
                    {
                        _enqueuedNextAction = true;
                        needEnqueue = true;
                    }
                }
                if (needEnqueue)
                {
                    _enqueueNextAction();
                }
            }

            /// <summary>
            /// Enqueue if not Pause.
            /// </summary>
            public void CheckAndEnqueue()
            {
                bool needEnqueue = false;
                lock (_lock)
                {
                    if ((!_enqueuedNextAction) && (!_paused))
                    {
                        _enqueuedNextAction = true;
                        needEnqueue = true;
                    }
                }
                if (needEnqueue)
                {
                    _enqueueNextAction();
                }
            }
        }
    }
}
